using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //在霸王虫破土冲击时筛选周围 Pawn，并用原版可存档飞行器将其径向击飞到合法远端格。
    internal static class HiveLordBurrowPushUtility
    {
        //复制地图 Pawn 列表后逐一击飞，避免飞行器生成期间修改原集合。
        public static void PushNearbyPawns(
            HiveLordProjectionThing owner,
            HiveLordCombatExtension settings)
        {
            if (!owner.Spawned)
            {
                return;
            }

            Map map = owner.Map;
            Vector3 center = owner.Position.ToVector3Shifted();
            List<Pawn> pawns = new List<Pawn>(map.mapPawns.AllPawnsSpawned);
            HashSet<IntVec3> reservedDestinations = new HashSet<IntVec3>();
            for (int index = 0; index < pawns.Count; index++)
            {
                Pawn pawn = pawns[index];
                if (pawn == null
                    || pawn.Dead
                    || !pawn.Spawned
                    || (owner.IsSummoned && !HiveLordTargetFinder.IsDamageableEnemy(owner, pawn))
                    || !pawn.Position.InHorDistOf(owner.Position, settings.emergencePushRadius))
                {
                    continue;
                }

                TryPushPawn(pawn, map, center, settings, reservedDestinations);
            }
        }

        //为单个 Pawn 选择沿冲击方向最远的合法格，并保留玩家当前选择状态生成原版眩晕飞行器。
        private static void TryPushPawn(
            Pawn pawn,
            Map map,
            Vector3 center,
            HiveLordCombatExtension settings,
            HashSet<IntVec3> reservedDestinations)
        {
            Vector3 direction = pawn.Position.ToVector3Shifted() - center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Gen.RandomHorizontalVector(1f);
            }

            direction.Normalize();
            float distance = Rand.Range(
                settings.emergencePushMinDistance,
                settings.emergencePushMaxDistance);
            if (!TryFindLandingCell(
                    pawn,
                    map,
                    center,
                    direction,
                    distance,
                    reservedDestinations,
                    out IntVec3 destination))
            {
                return;
            }

            bool selected = Find.Selector.IsSelected(pawn);
            PawnFlyer flyer = PawnFlyer.MakeFlyer(
                ThingDefOf.PawnFlyer_Stun,
                pawn,
                destination,
                null,
                null);
            if (flyer == null)
            {
                return;
            }

            reservedDestinations.Add(destination);
            GenSpawn.Spawn(flyer, destination, map);
            if (selected)
            {
                Find.Selector.Select(pawn, false, false);
            }
        }

        //从最大击飞距离向内搜索，并在目标附近寻找可站立、无遮挡且未被其他单位预订的落点。
        private static bool TryFindLandingCell(
            Pawn pawn,
            Map map,
            Vector3 center,
            Vector3 direction,
            float requestedDistance,
            HashSet<IntVec3> reservedDestinations,
            out IntVec3 destination)
        {
            float currentRadiusSquared = (pawn.Position.ToVector3Shifted() - center).sqrMagnitude;
            int maximumDistance = Mathf.CeilToInt(requestedDistance);
            int minimumDistance = Mathf.Max(4, Mathf.FloorToInt(requestedDistance * 0.55f));
            for (int distance = maximumDistance; distance >= minimumDistance; distance--)
            {
                IntVec3 expected = (pawn.Position.ToVector3Shifted() + direction * distance).ToIntVec3();
                foreach (IntVec3 candidate in GenRadial.RadialCellsAround(expected, 3f, true))
                {
                    if (IsValidLandingCell(
                            pawn,
                            map,
                            center,
                            currentRadiusSquared,
                            candidate,
                            reservedDestinations))
                    {
                        destination = candidate;
                        return true;
                    }
                }
            }

            destination = IntVec3.Invalid;
            return false;
        }

        //拒绝越墙、不可行走、被占用或没有真正远离冲击中心的候选格。
        private static bool IsValidLandingCell(
            Pawn pawn,
            Map map,
            Vector3 center,
            float currentRadiusSquared,
            IntVec3 candidate,
            HashSet<IntVec3> reservedDestinations)
        {
            if (!candidate.IsValid
                || !candidate.InBounds(map)
                || reservedDestinations.Contains(candidate)
                || !JumpUtility.ValidJumpTarget(pawn, map, candidate)
                || candidate.GetFirstPawn(map) != null
                || !GenSight.LineOfSight(pawn.Position, candidate, map, true))
            {
                return false;
            }

            return (candidate.ToVector3Shifted() - center).sqrMagnitude
                > currentRadiusSquared + 16f;
        }
    }
}
