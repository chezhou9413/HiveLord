using HarmonyLib;
using RimWorld;
using Verse;

namespace HiveLordLib
{
    //把未结束的挑战与地下霸王虫计入场景威胁，防止原版提前提示安全并允许重组。
    [HarmonyPatch(typeof(GenHostility), nameof(GenHostility.AnyHostileActiveThreatToPlayer))]
    internal static class HiveLordActiveThreatPatch
    {
        //保留原版敌人判定，仅补充准备阶段及没有地表受击体的霸王虫威胁。
        private static void Postfix(Map map, ref bool __result)
        {
            if (__result || map == null) return;
            if (map.Parent is HiveLordChallengeSite site
                && (site.Phase == HiveLordChallengePhase.PendingEntry
                    || site.Phase == HiveLordChallengePhase.ClearingSpores
                    || site.Phase == HiveLordChallengePhase.Fighting))
            {
                __result = true;
                return;
            }
            foreach (Thing thing in map.listerThings.ThingsOfDef(HiveLordDefOf.HiveLord_Projection))
            {
                if (thing is HiveLordProjectionThing boss && boss.Spawned && !boss.Destroyed
                    && !boss.IsDying && boss.CombatAiEnabled && boss.HostileTo(Faction.OfPlayer))
                {
                    __result = true;
                    return;
                }
            }
        }
    }
}
