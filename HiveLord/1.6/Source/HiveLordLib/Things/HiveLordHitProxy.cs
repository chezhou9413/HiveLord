using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HiveLordLib
{
    //作为霸王虫地表阶段的巨大可选中受击体，转发伤害倍率与死亡通知。
    public sealed class HiveLordHitProxy : Building, IAttackTarget, IAttackTargetSearcher
    {
        private HiveLordProjectionThing owner;

        Thing IAttackTarget.Thing => this;
        Thing IAttackTargetSearcher.Thing => this;
        public LocalTargetInfo TargetCurrentlyAimingAt => owner?.CombatTarget ?? LocalTargetInfo.Invalid;
        public float TargetPriorityFactor => 4f;
        public Verb CurrentEffectiveVerb => null;
        public LocalTargetInfo LastAttackedTarget => LocalTargetInfo.Invalid;
        public int LastAttackTargetTick => -99999;

        //选中召唤体地表代理时显示锚点保存的生命值和剩余存续期。
        public override string GetInspectString()
        {
            return owner != null && owner.IsSummoned ? owner.GetInspectString() : base.GetInspectString();
        }

        //把代理绑定到负责视觉和战斗状态的霸王虫锚点。
        public void BindOwner(HiveLordProjectionThing configuredOwner)
        {
            owner = configuredOwner;
        }

        //只要代理仍在地表且对应霸王虫存活，就向原版 AI 报告为有效主动威胁。
        public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
        {
            return !Spawned
                || Destroyed
                || HitPoints <= 0
                || owner == null
                || owner.Destroyed
                || owner.IsDying
                || owner.VisualState == HiveLordVisualState.Underground;
        }

        //保存与霸王虫锚点的对应关系，使地表阶段读档后仍能转发生命状态。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref owner, "hiveLordProjectionOwner");
        }

        //隐藏代理自身贴图，地图上的虫体继续由捕获面片负责绘制。
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
        }

        //在原版伤害结算前应用受伤倍率，并阻止死亡期间继续受击。
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            base.PreApplyDamage(ref dinfo, out absorbed);
            if (absorbed) return;
            if (owner == null || owner.IsDying || owner.Destroyed)
            {
                absorbed = true;
                return;
            }
            if (dinfo.Def.harmsHealth)
            {
                dinfo.SetAmount(dinfo.Amount * HiveLordDifficulty.IncomingDamage);
            }
        }

        //让选中巨大代理时仍可访问霸王虫锚点提供的开发者控制命令。
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (owner == null || owner.Destroyed)
            {
                yield break;
            }

            foreach (Gizmo gizmo in owner.GetGizmos())
            {
                yield return gizmo;
            }
        }

        //代理被伤害摧毁时通知锚点结束霸王虫；普通入土回收不触发死亡。
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            HiveLordProjectionThing boundOwner = owner;
            base.Destroy(mode);
            if (mode == DestroyMode.KillFinalize
                && boundOwner != null
                && !boundOwner.Destroyed)
            {
                boundOwner.NotifyHitProxyKilled(this);
            }
        }
    }
}
