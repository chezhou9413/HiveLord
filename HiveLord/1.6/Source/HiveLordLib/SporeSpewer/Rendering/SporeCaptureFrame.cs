using System;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //持有一份虫体颜色与独立描边遮罩，供存活虫体共享或保存各倒塌姿势。
    internal sealed class SporeCaptureFrame : IDisposable
    {
        internal const float ProjectionSize = 24f;
        internal readonly RenderTexture Texture;
        internal readonly HiveLordBodyMaskCapture Mask;
        internal int LastCaptureTick = int.MinValue;
        private readonly Material body;
        private readonly Material shadow;

        //按分辨率建立颜色、遮罩和输出材质，倒塌帧不持有摄像机或模型。
        internal SporeCaptureFrame(int resolution)
        {
            Texture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32)
            {
                name = "SporeSpewer_CaptureFrame",
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            if (!Texture.Create()) throw new InvalidOperationException("HiveLord_Error_SporeTexture".Translate().ToString());
            Mask = new HiveLordBodyMaskCapture();
            Mask.EnsureTexture(resolution);
            body = CreateOutput(2, SporeSpewerView.BodyRenderQueue, CompareFunction.Always, resolution);
            shadow = CreateOutput(1, 2990, CompareFunction.LessEqual, resolution);
        }

        //将捕获姿势按统一显示比例绘制到地图，保持橙雾覆盖虫体及黑色轮廓。
        internal void Draw(Vector3 position)
        {
            Vector3 center = position + SporeSpewerView.PlaneOffset;
            float drawSize = ProjectionSize * SporeSpewerView.RenderScale;
            Vector3 scale = new Vector3(drawSize, 1f, drawSize);
            center.y = AltitudeLayer.Shadows.AltitudeFor();
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(center, Quaternion.identity, scale), shadow, 0);
            center.y = AltitudeLayer.MoteOverhead.AltitudeFor();
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(center, Quaternion.identity, scale), body, 0);
        }

        //配置已有卡通输出着色器，按纹理分辨率维持一致的地图描边宽度。
        private Material CreateOutput(int mode, int queue, CompareFunction depth, int resolution)
        {
            var material = new Material(HiveLordAssets.RequireOutputShader())
            {
                name = "SporeSpewer_Output_" + mode,
                hideFlags = HideFlags.HideAndDontSave,
                renderQueue = Mathf.Min(queue, ShaderDatabase.MetaOverlay.renderQueue - 1)
            };
            material.SetTexture("_MainTex", Texture);
            material.SetTexture("_ModelMaskTex", Mask.Texture);
            material.SetFloat("_UseModelMask", 1f);
            material.SetFloat("_OutputLayerMode", mode);
            material.SetFloat("_ZTestMode", (float)depth);
            material.SetFloat("_ApplyMapLighting", 0f);
            material.SetFloat("_OutlineWidth", 10f * resolution / 2048f);
            return material;
        }

        //释放一次捕获结果的纹理与材质，不影响共享摄像机和其他虫体。
        public void Dispose()
        {
            Texture.Release();
            UnityEngine.Object.Destroy(Texture);
            Mask.Dispose();
            UnityEngine.Object.Destroy(body);
            UnityEngine.Object.Destroy(shadow);
        }
    }
}
