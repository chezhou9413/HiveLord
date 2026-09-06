using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HiveLordLib.SporeSpewer
{
    //提供静止虫族攻击目标、爆炸限定生命伤害和地图孢子源生命周期。
    public sealed class SporeSpewerThing : Building, IAttackTarget, IAttackTargetSearcher
    {
        Thing IAttackTarget.Thing => this;
        Thing IAttackTargetSearcher.Thing => this;
        public LocalTargetInfo TargetCurrentlyAimingAt => LocalTargetInfo.Invalid;
        public float TargetPriorityFactor => 2f;
        public Verb CurrentEffectiveVerb => null;
        public LocalTargetInfo LastAttackedTarget => LocalTargetInfo.Invalid;
        public int LastAttackTargetTick => -99999;

        //将存活且已生成的孢子喷涌虫报告为敌对威胁。
        public bool ThreatDisabled(IAttackTargetSearcher disabledFor)
        {
            return !Spawned || Destroyed || HitPoints <= 0;
        }

        //在实体生成后确定虫族归属并登记地图影响。
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            SetFaction(Faction.OfInsects);
            map.GetComponent<SporeSpewerMapComponent>().Register(this);
        }

        //实体离开地图时立即注销源，允许最后一个源停止减益。
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map.GetComponent<SporeSpewerMapComponent>().Unregister(this);
            base.DeSpawn(mode);
        }

        //阻止一切真实爆炸作用域以外的生命伤害及非生命爆炸效果。
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            if (!SporeExplosionDamagePatch.Allows(this, dinfo))
            {
                absorbed = true;
                return;
            }
            base.PreApplyDamage(ref dinfo, out absorbed);
        }

        //在致死移除前立即结束孢子源，并交由地图组件播放独立倒塌表现。
        public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
        {
            if (Spawned)
            {
                SporeSpewerMapComponent spores = Map.GetComponent<SporeSpewerMapComponent>();
                if (mode == DestroyMode.KillFinalize) spores.BeginCollapse(this);
                else spores.Unregister(this);
            }
            base.Destroy(mode);
        }

        //隐藏原版建筑贴图，由地图共享捕获器绘制卡通虫体。
        protected override void DrawAt(Vector3 drawLoc, bool flip = false) { }

        //按游戏刻度在自身位置持续喷涌橙色孢子。
        protected override void Tick()
        {
            base.Tick();
            if (Spawned && Map == Find.CurrentMap && this.IsHashIntervalTick(12))
                SporeVisualEffects.EmitSource(Map, DrawPos);
        }

        //在选中信息中说明固定生命值和爆炸限定弱点。
        public override string GetInspectString()
        {
            return "HiveLord_Spore_Inspect".Translate(HitPoints, MaxHitPoints).ToString();
        }
    }
}
