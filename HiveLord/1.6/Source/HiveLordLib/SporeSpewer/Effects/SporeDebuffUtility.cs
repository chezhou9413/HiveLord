using RimWorld;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //判定单位是否受地图孢子影响，并同步唯一健康状态与属性缓存。
    internal static class SporeDebuffUtility
    {
        //只将同地图已生成且存活的单位判为孢子影响对象。
        internal static bool IsAffected(Pawn pawn)
        {
            return pawn != null && pawn.Spawned && !pawn.Dead
                && pawn.Map.GetComponent<SporeSpewerMapComponent>().IsActive;
        }

        //同步健康状态，并在变化时清除最终移速与视觉能力缓存。
        internal static void Synchronize(Pawn pawn, bool active)
        {
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(SporeSpewerDefOf.HiveLord_SporeInterference);
            if (active && hediff == null)
                pawn.health.AddHediff(SporeSpewerDefOf.HiveLord_SporeInterference);
            else if (!active && hediff != null)
                pawn.health.RemoveHediff(hediff);
            else
                return;

            pawn.health.capacities.Notify_CapacityLevelsDirty();
            StatDefOf.MoveSpeed.Worker.ClearCacheForThing(pawn);
        }
    }
}
