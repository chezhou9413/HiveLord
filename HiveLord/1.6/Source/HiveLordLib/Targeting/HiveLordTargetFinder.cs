using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HiveLordLib
{
    //为霸王虫筛选最近的敌对 Pawn 或炮塔，并统一验证目标有效性。
    internal static class HiveLordTargetFinder
    {
        //在整张地图优先查找最近敌对 Pawn，没有 Pawn 时再查找敌对炮塔。
        public static Thing FindClosest(HiveLordProjectionThing owner, HiveLordCombatExtension settings)
        {
            Thing closest = null;
            float closestDistanceSquared = settings.targetSearchRadius * settings.targetSearchRadius;
            IReadOnlyList<Pawn> pawns = owner.Map.mapPawns.AllPawnsSpawned;
            for (int index = 0; index < pawns.Count; index++)
            {
                Consider(owner, pawns[index], false, ref closest, ref closestDistanceSquared);
            }

            if (closest != null)
            {
                return closest;
            }

            closestDistanceSquared = settings.targetSearchRadius * settings.targetSearchRadius;
            List<Building> colonistBuildings = owner.Map.listerBuildings.allBuildingsColonist;
            for (int index = 0; index < colonistBuildings.Count; index++)
            {
                Consider(owner, colonistBuildings[index], false, ref closest, ref closestDistanceSquared);
            }

            List<Building> nonColonistBuildings = owner.Map.listerBuildings.allBuildingsNonColonist;
            for (int index = 0; index < nonColonistBuildings.Count; index++)
            {
                Consider(owner, nonColonistBuildings[index], false, ref closest, ref closestDistanceSquared);
            }

            return closest;
        }

        //验证锁定目标仍在同一地图、仍敌对并位于允许保留的距离内。
        public static bool IsLockable(
            HiveLordProjectionThing owner,
            Thing target,
            float maximumRange,
            bool allowDownedPawn)
        {
            if (target == null || target.Destroyed || !target.Spawned || target.Map != owner.Map)
            {
                return false;
            }

            bool hostile = owner.IsSummoned
                ? (owner.Faction != target.Faction && (owner.HostileTo(target) || target.HostileTo(owner)))
                : AreHostile(owner.Faction, target.Faction);
            if (!hostile
                || owner.Position.DistanceToSquared(target.Position) > maximumRange * maximumRange)
            {
                return false;
            }

            Pawn pawn = target as Pawn;
            if (pawn != null)
            {
                return !pawn.Dead && (allowDownedPawn || !pawn.Downed);
            }

            if (target is HiveLordHitProxy proxy) return !proxy.ThreatDisabled(null);
            Building building = target as Building;
            return building != null && building.def.building != null && building.def.building.IsTurret;
        }

        //验证范围伤害候选是敌对 Pawn 或炮塔，允许倒地敌人受到波及。
        public static bool IsDamageableEnemy(HiveLordProjectionThing owner, Thing candidate)
        {
            return candidate != owner
                && IsLockable(owner, candidate, float.MaxValue, true);
        }

        //把一个合法候选按距离与当前最近结果进行比较。
        private static void Consider(
            HiveLordProjectionThing owner,
            Thing candidate,
            bool allowDownedPawn,
            ref Thing closest,
            ref float closestDistanceSquared)
        {
            if (!IsLockable(owner, candidate, owner.CombatSettings.targetSearchRadius, allowDownedPawn))
            {
                return;
            }

            float distanceSquared = owner.Position.DistanceToSquared(candidate.Position);
            if (distanceSquared > closestDistanceSquared)
            {
                return;
            }

            closest = candidate;
            closestDistanceSquared = distanceSquared;
        }

        //把任意一方声明的敌对关系都视作敌人，使非玩家敌对派系也能被锁定。
        private static bool AreHostile(Faction ownerFaction, Faction targetFaction)
        {
            return ownerFaction != null
                && targetFaction != null
                && ownerFaction != targetFaction
                && (ownerFaction.HostileTo(targetFaction) || targetFaction.HostileTo(ownerFaction));
        }
    }
}
