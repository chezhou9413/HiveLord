using Verse;
using System;
using UnityEngine;

namespace HiveLordLib
{
    //用独立单通道缓冲捕获虫体轮廓，隔离粒子混色与地面阴影对描边的影响。
    internal sealed class HiveLordBodyMaskCapture : IDisposable
    {
        private readonly Shader maskShader;
        private RenderTexture texture;
        public RenderTexture Texture => texture;

        //加载只输出虫体轮廓的替换着色器。
        public HiveLordBodyMaskCapture()
        {
            maskShader = HiveLordAssets.RequireBodyMaskShader();
        }

        //使用与主体相同的尺寸建立无需深度缓冲的线性单通道遮罩。
        public void EnsureTexture(int resolution)
        {
            if (texture != null && texture.width == resolution) return;
            Dispose();
            texture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear)
            {
                name = "HiveLord_BodyMask_" + resolution,
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1
            };
            if (!texture.Create()) throw new InvalidOperationException("HiveLord_Error_MaskTexture".Translate().ToString());
        }

        //在同一姿势和相机下只捕获透明裁剪虫体，完成后恢复相机目标与活动缓冲。
        public void Capture(Camera camera)
        {
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                RenderTexture.active = texture;
                GL.Clear(false, true, Color.clear);
                //替换着色器仅匹配虫体的渲染类型，透明粒子与虫体原材质的投影阴影通道不参与绘制。
                camera.RenderWithShader(maskShader, "RenderType");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
            }
        }

        //释放遮罩缓冲，供缩放切换与地图舞台销毁共用。
        public void Dispose()
        {
            if (texture == null) return;
            texture.Release();
            UnityEngine.Object.Destroy(texture);
            texture = null;
        }
    }
}
