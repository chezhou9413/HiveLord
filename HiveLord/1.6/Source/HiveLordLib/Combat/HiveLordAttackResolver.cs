using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HiveLordLib
{
    //在攻击命中帧筛选范围内敌人并即时结算吐酸或砸地伤害。
    internal sealed class HiveLordAttackResolver
    {
        private readonly HashSet<Thing> victims = new HashSet<Thing>();
        private readonly List<IntVec3> impactedCells = new List<IntVec3>();

        //始终生成完整吐酸反馈，仅在瞄准方向仍有视线时结算酸蚀伤害。
        public bool ResolveAcid(
            HiveLordProjectionThing owner,
            IReadOnlyList<IntVec3> areaCells,
            HiveLordCombatExtension settings)
        {
            IntVec3 aimCell = owner.CombatAimCell;
            if (!aimCell.IsValid || !aimCell.InBounds(owner.Map))
            {
                return false;
            }

            impactedCells.Clear();
            for (int index = 0; index < areaCells.Count; index++)
            {
                IntVec3 cell = areaCells[index];
                if (cell.InBounds(owner.Map))
                {
                    impactedCells.Add(cell);
                }
            }

            if (impactedCells.Count == 0)
            {
                return false;
            }

            HiveLordCombatEffects.SpawnAcidImpact(owner.Map, impactedCells);
            if (!GenSight.LineOfSight(owner.Position, aimCell, owner.Map, true))
            {
                return true;
            }

            CollectVictims(owner, impactedCells);
            ApplyDamage(owner, DamageDefOf.AcidBurn, settings.acidDamage, settings.acidArmorPenetration);
            return true;
        }

        //沿虫体攻击方向的完整长条结算一次砸地伤害与地面冲击。
        public bool ResolveGroundSlam(
            HiveLordProjectionThing owner,
            IReadOnlyList<IntVec3> areaCells,
            HiveLordCombatExtension settings)
        {
            if (areaCells.Count == 0)
            {
                return false;
            }

            HiveLordCombatEffects.SpawnGroundSlamImpact(owner.Map, areaCells);
            owner.Map.GetComponent<HiveLordImpactWaveMapComponent>().AddSlam(owner);
            CollectVictims(owner, areaCells);
            ApplyDamage(owner, DamageDefOf.Blunt, settings.slamDamage, settings.slamArmorPenetration);
            return true;
        }

        //从实际受击格复制并去重合法敌人，避免伤害期间改动地图列表。
        private void CollectVictims(
            HiveLordProjectionThing owner,
            IReadOnlyList<IntVec3> areaCells)
        {
            victims.Clear();
            for (int cellIndex = 0; cellIndex < areaCells.Count; cellIndex++)
            {
                List<Thing> things = areaCells[cellIndex].GetThingList(owner.Map);
                for (int thingIndex = 0; thingIndex < things.Count; thingIndex++)
                {
                    Thing candidate = things[thingIndex];
                    if (HiveLordTargetFinder.IsDamageableEnemy(owner, candidate))
                    {
                        victims.Add(candidate);
                    }
                }
            }
        }

        //向已经筛选的敌人发送同一份伤害参数，并把霸王虫记为伤害来源。
        private void ApplyDamage(
            HiveLordProjectionThing owner,
            DamageDef damageDef,
            float amount,
            float armorPenetration)
        {
            foreach (Thing victim in victims)
            {
                if (victim.Destroyed)
                {
                    continue;
                }

                victim.TakeDamage(new DamageInfo(
                    damageDef,
                    amount * HiveLordDifficulty.AttackPower,
                    armorPenetration,
                    instigator: owner,
                    intendedTarget: victim));
            }

            victims.Clear();
        }
    }
}
