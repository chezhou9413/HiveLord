using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //在移动属性最终计算后执行固定扣减，避免被速度倍率再次放大。
    [HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValue), new[] { typeof(StatRequest), typeof(bool) })]
    internal static class SporeMoveSpeedPatch
    {
        //仅对受孢子影响单位的最终移速减去每秒零点三格并保留最低速度。
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(StatRequest req, StatDef ___stat, ref float __result)
        {
            if (___stat == StatDefOf.MoveSpeed && SporeDebuffUtility.IsAffected(req.Thing as Pawn))
                __result = Math.Max(0.1f, __result - 0.3f);
        }
    }
}
