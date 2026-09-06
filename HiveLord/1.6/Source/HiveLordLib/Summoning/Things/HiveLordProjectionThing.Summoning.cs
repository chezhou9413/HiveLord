using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //保存召唤体的阵营和到期时间，提供独立体型、生命与攻击范围。
    public sealed partial class HiveLordProjectionThing
    {
        private int summonEndTick = -1;
        public bool IsSummoned => def == HiveLordSummonDefOf.HiveLord_Summoned;
        public float SizeFactor => IsSummoned ? 0.5f : 1f;
        internal int ConfiguredMaximumHealth => IsSummoned
            ? Mathf.Max(1, Mathf.RoundToInt(HiveLordDifficulty.MaxHitPoints / 3f)) : HiveLordDifficulty.MaxHitPoints;
        internal ThingDef HitProxyDef => IsSummoned ? HiveLordSummonDefOf.HiveLord_SummonedHitProxy : HiveLordDefOf.HiveLord_HitProxy;

        //在生成之前绑定使用者阵营并设定一日存续期。
        internal void ConfigureSummon(Pawn caster)
        {
            if (Spawned || !IsSummoned || caster.Faction == null)
                throw new InvalidOperationException("HiveLord_Error_SummonFaction".Translate().ToString());
            SetFaction(caster.Faction);
            summonEndTick = Find.TickManager.TicksGame + GenDate.TicksPerDay;
        }

        //在召唤落点直接出土，随后恢复全图自动索敌。
        internal void BeginSummonedEncounter()
        {
            CombatController.BeginSummonedEncounter();
            SynchronizeHitProxy();
        }

        //到期立即移除召唤体及代理，普通消失不进入任务击杀结算。
        private bool TryExpireSummon()
        {
            if (!IsSummoned || Find.TickManager.TicksGame < summonEndTick) return false;
            Destroy(DestroyMode.Vanish);
            return true;
        }

        //保存绝对到期时刻，使暂停、切图和读档遵循同一游戏计时。
        private void ExposeSummonData()
        {
            Scribe_Values.Look(ref summonEndTick, "summonEndTick", -1);
        }

        //展示召唤体剩余时间与共享生命值。
        public override string GetInspectString()
        {
            string text = base.GetInspectString();
            if (!IsSummoned) return text;
            return text + (text.NullOrEmpty() ? "" : "\n") + "HiveLord_Summon_Inspect".Translate(CurrentHealth, MaximumHealth,
                Mathf.Max(0, summonEndTick - Find.TickManager.TicksGame).ToStringTicksToPeriod());
        }
    }
}
