using HarmonyLib;
using RimWorld;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //在孢子源出现后关闭已经打开的原版地图搜索，停止结果更新与定位箭头。
    [HarmonyPatch(typeof(Dialog_MapSearch), "ShouldClose", MethodType.Getter)]
    internal static class SporeOpenSearchPatch
    {
        //保留原版切图关闭规则，同时让受孢子影响的搜索窗口自行关闭。
        private static void Postfix(Map ___map, ref bool __result)
        {
            __result |= SporeMapSearchPatch.IsBlocked(___map);
        }
    }
}
