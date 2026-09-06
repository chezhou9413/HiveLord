using HarmonyLib;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //单位进入地图后立即同步孢子状态，防止先前地图的属性缓存沿用。
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
    internal static class SporePawnSpawnPatch
    {
        //在原版生成流程完成后按照目标地图的孢子源同步减益。
        private static void Postfix(Pawn __instance)
        {
            SporeDebuffUtility.Synchronize(__instance, SporeDebuffUtility.IsAffected(__instance));
        }
    }
}
