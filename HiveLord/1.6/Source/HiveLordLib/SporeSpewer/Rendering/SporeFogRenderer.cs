using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //绘制独立于天气的橙色双层流动雾，材质状态仅归属单个地图。
    internal sealed class SporeFogRenderer : IDisposable
    {
        private readonly Material material;

        //创建世界坐标采样的透明雾材质，不改动原版天气资源。
        internal SporeFogRenderer()
        {
            material = new Material(SporeSpewerAssets.RequireFogShader())
            {
                name = "SporeSpewer_MapFog",
                hideFlags = HideFlags.HideAndDontSave,
                //雾层覆盖孢子虫及描边，选中标记继续在雾层之后显示。
                renderQueue = SporeSpewerView.FogRenderQueue
            };
        }

        //传递游戏时间与地图浓度，暂停时噪声流动和浓度同时冻结。
        internal void Draw(Map map, float density)
        {
            material.SetFloat("_Density", density);
            material.SetFloat("_GameTime", Find.TickManager.TicksGame / 60f);
            SkyOverlay.DrawWorldOverlay(map, material, AltitudeLayer.Weather.AltitudeFor());
        }

        //释放地图专属雾材质。
        public void Dispose()
        {
            UnityEngine.Object.Destroy(material);
        }
    }
}
