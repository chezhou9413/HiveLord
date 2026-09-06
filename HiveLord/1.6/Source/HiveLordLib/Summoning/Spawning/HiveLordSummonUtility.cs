using System;
using Verse;

namespace HiveLordLib
{
    //校验技能落点并创建一只属于使用者阵营的限时霸王虫。
    public static class HiveLordSummonUtility
    {
        //拒绝无阵营使用者、越界、屋顶或已有建筑占据的召唤位置。
        public static AcceptanceReport CanSpawn(Pawn caster, IntVec3 cell)
        {
            if (caster == null || !caster.Spawned || caster.Faction == null) return "HiveLord_Summon_NeedCaster".Translate().ToString();
            Map map = caster.Map;
            CellRect rect = CellRect.CenteredOn(cell, HiveLordSummonDefOf.HiveLord_SummonedHitProxy.size);
            if (!rect.InBounds(map)) return "HiveLord_Summon_MapEdge".Translate().ToString();
            if (!cell.Walkable(map) || cell.Fogged(map)) return "HiveLord_Summon_BlockedCell".Translate().ToString();
            foreach (IntVec3 occupied in rect)
            {
                if (occupied.Roofed(map)) return "HiveLord_Summon_Roofed".Translate().ToString();
                foreach (Thing thing in occupied.GetThingList(map))
                    if (thing is Building) return "HiveLord_Summon_Occupied".Translate().ToString();
            }
            return true;
        }

        //生成独立定义的召唤体并出土，生成失败时清理本次实体并报告原因。
        public static HiveLordProjectionThing Spawn(Pawn caster, IntVec3 cell)
        {
            AcceptanceReport report = CanSpawn(caster, cell);
            if (!report.Accepted) throw new InvalidOperationException(report.Reason);
            var summon = (HiveLordProjectionThing)ThingMaker.MakeThing(HiveLordSummonDefOf.HiveLord_Summoned);
            summon.ConfigureSummon(caster);
            try
            {
                GenSpawn.Spawn(summon, cell, caster.Map, Rot4.North, WipeMode.VanishOrMoveAside);
                summon.BeginSummonedEncounter();
                return summon;
            }
            catch
            {
                if (summon.Spawned && !summon.Destroyed) summon.Destroy(DestroyMode.Vanish);
                throw;
            }
        }
    }
}
