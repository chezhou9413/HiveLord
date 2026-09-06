using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //用一个远端舞台为存活孢子虫和各倒塌姿势轮流捕获颜色及轮廓。
    internal sealed class SporeSpewerCapture : IDisposable
    {
        private readonly GameObject root;
        private readonly Transform collapsePivot;
        private readonly Camera camera;
        private readonly SporeCaptureFrame livingFrame;
        private readonly Dictionary<SporeCollapseState, SporeCaptureFrame> collapseFrames =
            new Dictionary<SporeCollapseState, SporeCaptureFrame>();
        private readonly List<Material> modelMaterials = new List<Material>();

        //创建无 Animator 的静态虫体、透明摄像机和独立轮廓遮罩。
        internal SporeSpewerCapture()
        {
            GameObject prefab = SporeSpewerAssets.RequirePrefab();
            root = new GameObject("SporeSpewer_SharedCapture") { hideFlags = HideFlags.HideAndDontSave };
            root.transform.position = new Vector3(12000f, 0f, 12000f);
            collapsePivot = new GameObject("SporeSpewer_CollapsePivot").transform;
            collapsePivot.SetParent(root.transform, false);
            GameObject model = UnityEngine.Object.Instantiate(prefab, collapsePivot, false);
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                //材质副本承载当前游戏时间的发光参数，不写回资源包材质。
                foreach (Material material in renderer.materials) modelMaterials.Add(material);
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            GameObject cameraObject = new GameObject("SporeSpewer_Camera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = SporeSpewerView.CameraPosition;
            cameraObject.transform.localRotation = SporeSpewerView.CameraRotation;
            camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = SporeCaptureFrame.ProjectionSize / 2f;
            camera.aspect = 1f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.renderingPath = RenderingPath.Forward;
            camera.useOcclusionCulling = false;
            livingFrame = new SporeCaptureFrame(2048);
            camera.targetTexture = livingFrame.Texture;
            livingFrame.Mask.Capture(camera);
        }

        //按游戏时间缓慢改变结节发光强度，每十五刻仅捕获一次共享画面。
        internal void UpdateCapture()
        {
            int ticks = Find.TickManager.TicksGame;
            if (livingFrame.LastCaptureTick != int.MinValue && ticks - livingFrame.LastCaptureTick < 15) return;
            float pulse = 0.35f + 0.25f * Mathf.Sin(ticks / 60f * 1.2f);
            foreach (Material material in modelMaterials) material.SetFloat("_EmissionPulse", pulse);
            RenderTexture previous = RenderTexture.active;
            try { camera.Render(); }
            finally { RenderTexture.active = previous; }
            livingFrame.LastCaptureTick = ticks;
        }

        //按显示比例缩放共享阴影与虫体，并在雾层之前绘制到实例位置。
        internal void Draw(SporeSpewerThing source)
        {
            livingFrame.Draw(source.DrawPos);
        }

        //复用摄像机捕获真实倾倒模型，各死亡实例仅保存较低分辨率的独立画面。
        internal void DrawCollapse(SporeCollapseState state)
        {
            if (!collapseFrames.TryGetValue(state, out SporeCaptureFrame frame))
            {
                frame = new SporeCaptureFrame(768);
                collapseFrames.Add(state, frame);
            }
            int ticks = Find.TickManager.TicksGame;
            if (frame.LastCaptureTick == int.MinValue || ticks - frame.LastCaptureTick >= 2)
            {
                CaptureCollapse(state, frame);
                frame.LastCaptureTick = ticks;
            }
            frame.Draw(state.Position);
        }

        //为颜色和遮罩使用同一倾倒姿势及地面裁剪，结束后恢复存活虫体的捕获状态。
        private void CaptureCollapse(SporeCollapseState state, SporeCaptureFrame frame)
        {
            RenderTexture previousActive = RenderTexture.active;
            Matrix4x4 previousProjection = camera.projectionMatrix;
            try
            {
                collapsePivot.localRotation = state.Rotation;
                collapsePivot.localPosition = Vector3.down * state.Sink;
                foreach (Material material in modelMaterials) material.SetFloat("_EmissionPulse", 0f);
                camera.targetTexture = frame.Texture;
                camera.projectionMatrix = GroundClippedProjection(previousProjection);
                camera.Render();
                frame.Mask.Capture(camera);
            }
            finally
            {
                collapsePivot.localRotation = Quaternion.identity;
                collapsePivot.localPosition = Vector3.zero;
                camera.projectionMatrix = previousProjection;
                camera.targetTexture = livingFrame.Texture;
                RenderTexture.active = previousActive;
            }
        }

        //把正交相机的远裁剪面倾斜到舞台地面，隐藏下沉部分而不改变投影位置。
        private Matrix4x4 GroundClippedProjection(Matrix4x4 projection)
        {
            Matrix4x4 view = camera.worldToCameraMatrix;
            Vector3 normal = view.MultiplyVector(Vector3.up).normalized;
            Vector3 point = view.MultiplyPoint(root.transform.position + Vector3.down * 0.05f);
            Vector4 plane = new Vector4(normal.x, normal.y, normal.z, -Vector3.Dot(normal, point));
            //相机位于地面上方，保留地面朝向相机的一侧；用近面角点缩放远裁剪方程。
            Vector4 corner = projection.inverse * new Vector4(Mathf.Sign(plane.x), Mathf.Sign(plane.y), -1f, 1f);
            Vector4 clip = plane * (2f / Vector4.Dot(plane, corner));
            projection.SetRow(2, projection.GetRow(3) - clip);
            return projection;
        }

        //倒塌结束后释放对应画面，其余存活和死亡虫体继续显示。
        internal void ReleaseCollapse(SporeCollapseState state)
        {
            if (!collapseFrames.TryGetValue(state, out SporeCaptureFrame frame)) return;
            frame.Dispose();
            collapseFrames.Remove(state);
        }

        //释放共享舞台、材质副本和两张捕获纹理。
        public void Dispose()
        {
            //延迟销毁前先停用舞台，避免同帧切换地图时旧模型混入新摄像机。
            root.SetActive(false);
            camera.targetTexture = null;
            livingFrame.Dispose();
            foreach (SporeCaptureFrame frame in collapseFrames.Values) frame.Dispose();
            collapseFrames.Clear();
            foreach (Material material in modelMaterials) UnityEngine.Object.Destroy(material);
            UnityEngine.Object.Destroy(root);
        }
    }
}
