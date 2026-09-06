using System;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace HiveLordLib
{
    //维护远端霸王虫、捕获摄像机、动态分辨率 RT 和地图双层面片绘制资源。
    internal sealed class HiveLordProjectionStage : IDisposable
    {
        private const float OriginalProjectionSize = 64f;
        private const float ProjectionCoverageMultiplier = 3f;
        internal const float ProjectionSize = OriginalProjectionSize * ProjectionCoverageMultiplier;
        private const float OriginalFieldOfView = 28f;
        private const int NearResolution = 8192;
        private const int FarResolution = 4096;
        private const float NearFramesPerSecond = 60f;
        private const float FarFramesPerSecond = 60f;
        private const int ShadowOutputMode = 1;
        private const int SubjectOutputMode = 2;
        private const int SubjectRenderQueue = 3590;
        private const float GroundClipTolerance = 0.05f;
        private static readonly System.Collections.Generic.HashSet<int> StageSlots = new System.Collections.Generic.HashSet<int>();
        private readonly int stageSlot;
        private static readonly Vector3 CaptureCameraOffset = new Vector3(18f, 225f, 0f);
        private static readonly Quaternion CaptureCameraRotation = Quaternion.Euler(90f, 0f, 0f);
        private static readonly int MainTextureId = Shader.PropertyToID("_MainTex");
        private static readonly int ModelMaskTextureId = Shader.PropertyToID("_ModelMaskTex");
        private static readonly int UseModelMaskId = Shader.PropertyToID("_UseModelMask");
        private static readonly int OutputModeId = Shader.PropertyToID("_OutputLayerMode");
        private static readonly int ZTestModeId = Shader.PropertyToID("_ZTestMode");
        private static readonly int ApplyMapLightingId = Shader.PropertyToID("_ApplyMapLighting");

        private readonly GameObject stageRoot;
        private readonly GameObject modelInstance;
        private readonly Camera captureCamera;
        private readonly Material shadowMaterial;
        private readonly Material subjectMaterial;
        private readonly HiveLordVisualController visualController;
        private readonly HiveLordMapLightMesh subjectLightingMesh;
        private readonly HiveLordBodyMaskCapture bodyMaskCapture;
        private RenderTexture renderTexture;
        private int renderResolution;
        private float nextRenderTime;
        private bool visible;
        private bool disposed;
        private bool hasCapturedFrame;
        private int capturedRevision = int.MinValue;
        private IntVec3 capturedPosition = IntVec3.Invalid;

        //从 CL 资源创建独立捕获舞台，并验证 Animator、粒子和输出 Shader。
        public HiveLordProjectionStage()
        {
            GameObject capturePrefab = HiveLordAssets.RequireCapturePrefab();
            Shader outputShader = HiveLordAssets.RequireOutputShader();
            bodyMaskCapture = new HiveLordBodyMaskCapture();

            stageRoot = new GameObject("HiveLord_RemoteCaptureStage")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            while (StageSlots.Contains(stageSlot)) stageSlot++;
            StageSlots.Add(stageSlot);
            //不同实例使用相隔512单位的舞台，避免相机捕获到另一只虫体或粒子。
            stageRoot.transform.position = new Vector3(10000f + stageSlot * 512f, 0f, 10000f);

            modelInstance = UnityEngine.Object.Instantiate(capturePrefab);
            modelInstance.name = "HiveLord_CaptureInstance";
            modelInstance.hideFlags = HideFlags.HideAndDontSave;
            modelInstance.transform.SetParent(stageRoot.transform, false);
            modelInstance.transform.localPosition = Vector3.zero;
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            Animator animator = modelInstance.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                throw new InvalidOperationException("HiveLord_Error_Animator".Translate().ToString());
            }

            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            ParticleSystem[] particles = modelInstance.GetComponentsInChildren<ParticleSystem>(true);
            visualController = new HiveLordVisualController(animator, particles);

            GameObject cameraObject = new GameObject("HiveLord_CaptureCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            cameraObject.transform.SetParent(stageRoot.transform, false);
            cameraObject.transform.localPosition = CaptureCameraOffset;
            cameraObject.transform.localRotation = CaptureCameraRotation;
            captureCamera = cameraObject.AddComponent<Camera>();
            ConfigureCaptureCamera(captureCamera);

            shadowMaterial = CreateOutputMaterial(
                outputShader,
                "HiveLord_ShadowOutput",
                ShadowOutputMode,
                2990,
                CompareFunction.LessEqual,
                false);
            subjectMaterial = CreateOutputMaterial(
                outputShader,
                "HiveLord_SubjectOutput",
                SubjectOutputMode,
                SubjectRenderQueue,
                CompareFunction.Always,
                true);
            subjectLightingMesh = new HiveLordMapLightMesh();
            SetVisible(false);
        }

        //同步动画与暂停状态，并按当前缩放决定 RT 分辨率和捕获频率。
        public void UpdateAndRender(HiveLordProjectionThing thing)
        {
            ThrowIfDisposed();
            SetVisible(true);
            UpdateFacing(thing);
            bool paused = Find.TickManager == null || Find.TickManager.Paused;
            visualController.Update(thing, paused);
            Vector3 modelPosition = modelInstance.transform.localPosition;
            modelPosition.y = visualController.GetModelVerticalOffset(thing);
            modelInstance.transform.localPosition = modelPosition;

            bool nearView = Find.CameraDriver.CurrentZoom <= CameraZoomRange.Close;
            int desiredResolution = nearView ? NearResolution : FarResolution;
            if (thing.IsSummoned) desiredResolution /= 2;
            float framesPerSecond = nearView ? NearFramesPerSecond : FarFramesPerSecond;
            EnsureRenderTexture(desiredResolution);

            float now = Time.realtimeSinceStartup;
            //状态切换或地下换位后必须捕获当前姿势，不能把上一位置的地表画面贴到新位置。
            if (hasCapturedFrame && now < nextRenderTime
                && capturedRevision == thing.StateRevision && capturedPosition == thing.Position)
            {
                return;
            }

            RenderCapture();
            hasCapturedFrame = true;
            capturedRevision = thing.StateRevision;
            capturedPosition = thing.Position;
            nextRenderTime = now + 1f / framesPerSecond;
        }

        //把同一张 RT 分别以地面阴影层和置顶主体层绘制到地图。
        public void Draw(HiveLordProjectionThing thing)
        {
            if (disposed || renderTexture == null || !visible || !hasCapturedFrame)
            {
                return;
            }

            Vector3 viewportAnchor = captureCamera.WorldToViewportPoint(stageRoot.transform.position);
            if (viewportAnchor.z <= 0f)
            {
                throw new InvalidOperationException("HiveLord_Error_CameraAnchor".Translate().ToString());
            }

            float drawSize = ProjectionSize * thing.SizeFactor;
            Vector3 planeCenter = new Vector3(
                thing.DrawPos.x + (0.5f - viewportAnchor.x) * drawSize,
                0f,
                thing.DrawPos.z + (0.5f - viewportAnchor.y) * drawSize);
            Vector3 scale = new Vector3(drawSize, 1f, drawSize);
            subjectLightingMesh.Update(thing.Map, planeCenter, drawSize);

            planeCenter.y = AltitudeLayer.Shadows.AltitudeFor();
            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(planeCenter, Quaternion.identity, scale),
                shadowMaterial,
                0);

            planeCenter.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Graphics.DrawMesh(
                subjectLightingMesh.Mesh,
                Matrix4x4.TRS(planeCenter, Quaternion.identity, scale),
                subjectMaterial,
                0);
        }

        //启停远端舞台，使不可见地图不再更新 Animator、粒子和摄像机。
        public void SetVisible(bool shouldBeVisible)
        {
            if (disposed || (visible == shouldBeVisible && stageRoot.activeSelf == shouldBeVisible))
            {
                return;
            }

            visible = shouldBeVisible;
            hasCapturedFrame = false;
            nextRenderTime = 0f;
            if (stageRoot != null)
            {
                stageRoot.SetActive(shouldBeVisible);
            }

            if (shouldBeVisible)
            {
                visualController.RequestResync();
            }
        }

        //释放 RT、材质和整个远端 Unity 对象树。
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            ReleaseRenderTexture();
            if (shadowMaterial != null)
            {
                UnityEngine.Object.Destroy(shadowMaterial);
            }

            if (subjectMaterial != null)
            {
                UnityEngine.Object.Destroy(subjectMaterial);
            }

            if (stageRoot != null)
            {
                stageRoot.SetActive(false);
                UnityEngine.Object.Destroy(stageRoot);
            }
            StageSlots.Remove(stageSlot);

            subjectLightingMesh.Dispose();
        }

        //配置三倍覆盖且保持模型地图尺寸不变的透明捕获摄像机。
        private static void ConfigureCaptureCamera(Camera camera)
        {
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = false;
            float originalHalfFieldOfView = OriginalFieldOfView * 0.5f * Mathf.Deg2Rad;
            camera.fieldOfView = Mathf.Atan(
                Mathf.Tan(originalHalfFieldOfView) * ProjectionCoverageMultiplier)
                * 2f
                * Mathf.Rad2Deg;
            camera.nearClipPlane = 0.3f;
            //摄像机垂直向下，远裁剪面对应舞台地面；保留微小余量让地面上的投影阴影正常绘制。
            camera.farClipPlane = CaptureCameraOffset.y + GroundClipTolerance;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.depth = -100f;
        }

        //让远端模型在地图平面上持续朝向当前目标或最后保存的瞄准格。
        private void UpdateFacing(HiveLordProjectionThing thing)
        {
            IntVec3 aimCell = thing.CombatAimCell;
            if (!aimCell.IsValid)
            {
                return;
            }

            Vector3 direction = new Vector3(
                aimCell.x - thing.Position.x,
                0f,
                aimCell.z - thing.Position.z);
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            modelInstance.transform.localRotation = Quaternion.LookRotation(-direction.normalized, Vector3.up);
        }

        //创建输出材质并写入分层模式、深度测试和透明渲染队列。
        private static Material CreateOutputMaterial(
            Shader shader,
            string materialName,
            int outputMode,
            int renderQueue,
            CompareFunction zTest,
            bool applyMapLighting)
        {
            Material material = new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = renderQueue
            };
            material.SetFloat(OutputModeId, outputMode);
            material.SetFloat(ZTestModeId, (float)zTest);
            material.SetFloat(ApplyMapLightingId, applyMapLighting ? 1f : 0f);
            return material;
        }

        //在缩放档位变化时同时重建主体画面与同尺寸虫体遮罩。
        private void EnsureRenderTexture(int resolution)
        {
            if (renderTexture != null && renderResolution == resolution)
            {
                return;
            }

            ReleaseRenderTexture();
            renderTexture = new RenderTexture(
                resolution,
                resolution,
                24,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Default)
            {
                name = "HiveLord_Capture_" + resolution,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1
            };
            renderTexture.Create();
            renderResolution = resolution;
            captureCamera.targetTexture = renderTexture;
            shadowMaterial.SetTexture(MainTextureId, renderTexture);
            subjectMaterial.SetTexture(MainTextureId, renderTexture);
            bodyMaskCapture.EnsureTexture(resolution);
            shadowMaterial.SetTexture(ModelMaskTextureId, bodyMaskCapture.Texture);
            subjectMaterial.SetTexture(ModelMaskTextureId, bodyMaskCapture.Texture);
            shadowMaterial.SetFloat(UseModelMaskId, 1f);
            subjectMaterial.SetFloat(UseModelMaskId, 1f);
            nextRenderTime = 0f;
        }

        //在同一姿势下捕获虫体遮罩与完整画面，保证描边和粒子合成逐帧对齐。
        private void RenderCapture()
        {
            bodyMaskCapture.Capture(captureCamera);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = previous;
            captureCamera.Render();
        }

        //解除摄像机和材质绑定，并销毁当前 RT 的颜色与深度缓冲。
        private void ReleaseRenderTexture()
        {
            hasCapturedFrame = false;
            shadowMaterial.SetTexture(ModelMaskTextureId, null);
            subjectMaterial.SetTexture(ModelMaskTextureId, null);
            bodyMaskCapture.Dispose();
            if (renderTexture == null)
            {
                return;
            }

            captureCamera.targetTexture = null;
            shadowMaterial.SetTexture(MainTextureId, null);
            subjectMaterial.SetTexture(MainTextureId, null);
            renderTexture.Release();
            UnityEngine.Object.Destroy(renderTexture);
            renderTexture = null;
            renderResolution = 0;
        }

        //阻止已释放的远端舞台再次参与更新。
        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(HiveLordProjectionStage));
            }
        }
    }
}
