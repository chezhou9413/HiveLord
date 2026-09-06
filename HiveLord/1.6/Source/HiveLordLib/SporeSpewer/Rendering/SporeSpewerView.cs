using UnityEngine;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //统一斜视镜头、显示比例和雾层顺序，使虫体与孢子对齐并受浓雾遮盖。
    internal static class SporeSpewerView
    {
        internal const float RenderScale = 0.6f;
        private static readonly Vector3 FocusPoint = new Vector3(0f, 4f, 0f);
        internal static readonly Quaternion CameraRotation = Quaternion.Euler(40f, 25f, 0f);

        //将橙雾安排在选中标记之前合成，保留界面标记的可见性。
        internal static int FogRenderQueue => Mathf.Min(3480, ShaderDatabase.MetaOverlay.renderQueue - 2);

        //先绘制虫体及描边，再由橙雾覆盖完整虫体。
        internal static int BodyRenderQueue => FogRenderQueue - 1;

        //从斜上方注视虫体下半部，同时保留根部与顶部的捕获余量。
        internal static Vector3 CameraPosition => FocusPoint - CameraRotation * Vector3.forward * 70f;

        //补偿镜头注视点的高度，使地图占地中心仍对应模型地面原点。
        internal static Vector3 PlaneOffset => ProjectToMap(FocusPoint);

        //将约十一格高的孢子出口换算为当前斜视角度下的地图位置。
        internal static Vector3 SporeOutletOffset => ProjectToMap(new Vector3(0f, 11f, 0f));

        //把模型局部位置投影到摄像机平面，并按显示比例换算地图偏移。
        internal static Vector3 ProjectToMap(Vector3 localPoint)
        {
            Vector3 viewPoint = Quaternion.Inverse(CameraRotation) * localPoint;
            return new Vector3(viewPoint.x, 0f, viewPoint.y) * RenderScale;
        }
    }
}
