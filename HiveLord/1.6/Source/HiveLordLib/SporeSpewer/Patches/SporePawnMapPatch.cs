using HarmonyLib;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //在单位离图时即时移除孢子健康状态，避免世界单位保留地图减益。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.DeSpawn))]
    internal static class SporePawnMapPatch
    {
        //在地图引用清空前清理单位的健康状态与移动属性缓存。
        private static void Prefix(Pawn __instance)
        {
            SporeDebuffUtility.Synchronize(__instance, false);
        }
    }
}
