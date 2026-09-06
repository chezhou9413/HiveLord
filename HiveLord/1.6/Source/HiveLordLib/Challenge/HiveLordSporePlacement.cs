using System;
using System.Collections.Generic;
using HiveLordLib.SporeSpewer;
using RimWorld;
using Verse;
using Verse.AI;

namespace HiveLordLib
{
    //在地图发生任何孢子虫生成之前选齐五个分散、可达且合法的建筑位置。
    internal static class HiveLordSporePlacement
    {
        internal const int SourceCount = 5;

        //随机遍历地图内部候选，保证占地边界和入场人员均留出三十格距离。
        internal static List<IntVec3> FindPositions(Map map, Pawn entryPawn)
        {
            var candidates = new List<IntVec3>();
            for (int x = 32; x < map.Size.x - 32; x++)
                for (int z = 32; z < map.Size.z - 32; z++) candidates.Add(new IntVec3(x, 0, z));
            for (int i = candidates.Count - 1; i > 0; i--)
            {
                int other = Rand.RangeInclusive(0, i);
                IntVec3 cell = candidates[i];
                candidates[i] = candidates[other];
                candidates[other] = cell;
            }
            var selected = new List<IntVec3>(SourceCount);
            foreach (IntVec3 cell in candidates)
            {
                if (!IsSeparated(map, cell, selected) || !SporeSpewerSpawnUtility.CanSpawnAt(map, cell).Accepted) continue;
                if (!map.reachability.CanReach(entryPawn.Position, cell, PathEndMode.Touch,
                    TraverseMode.PassDoors, Danger.Deadly)) continue;
                selected.Add(cell);
                if (selected.Count == SourceCount) return selected;
            }
            throw new InvalidOperationException("HiveLord_Error_SporePlacement".Translate().ToString());
        }

        //排除距离已有候选或入场玩家单位过近的地点。
        private static bool IsSeparated(Map map, IntVec3 cell, List<IntVec3> selected)
        {
            foreach (IntVec3 other in selected)
                if (cell.DistanceToSquared(other) < 900) return false;
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                if (pawn.Faction == Faction.OfPlayer && cell.DistanceToSquared(pawn.Position) < 900) return false;
            return true;
        }
    }
}
