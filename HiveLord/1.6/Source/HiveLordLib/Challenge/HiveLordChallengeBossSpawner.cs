using System;
using Verse;

namespace HiveLordLib
{
    //为任务创建唯一霸王虫锚点并立即启动首轮出土，异常时清理未完成的生成。
    internal static class HiveLordChallengeBossSpawner
    {
        //寻找不覆盖建筑的临时锚点，生成后交由现有战斗落点规则进入出土阶段。
        internal static HiveLordProjectionThing Spawn(Map map, Pawn target)
        {
            if (map.listerThings.ThingsOfDef(HiveLordDefOf.HiveLord_Projection).Count != 0)
                throw new InvalidOperationException("HiveLord_Error_BossAlreadyPresent".Translate().ToString());
            IntVec3 anchor = IntVec3.Invalid;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.PositionHeld, 40f, true))
                if (cell.InBounds(map) && cell.Walkable(map) && cell.GetEdifice(map) == null)
                {
                    anchor = cell;
                    break;
                }
            if (!anchor.IsValid) throw new InvalidOperationException("HiveLord_Error_BossAnchor".Translate().ToString());
            var boss = (HiveLordProjectionThing)ThingMaker.MakeThing(HiveLordDefOf.HiveLord_Projection);
            try
            {
                GenSpawn.Spawn(boss, anchor, map, Rot4.North, WipeMode.VanishOrMoveAside);
                boss.BeginChallengeEncounter(target);
                return boss;
            }
            catch
            {
                if (boss.Spawned && !boss.Destroyed) boss.Destroy(DestroyMode.Vanish);
                throw;
            }
        }
    }
}
