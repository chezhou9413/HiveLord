using RimWorld;
using Verse;

namespace HiveLordLib
{
    //统一识别挑战队伍、入场目标和阻止通关地图清理的玩家人员。
    internal static class HiveLordChallengeParticipants
    {
        //识别尚能继续挑战的玩家自由殖民者。
        internal static bool IsActive(Pawn pawn)
        {
            return !pawn.Destroyed && !pawn.Dead && !pawn.Downed
                && pawn.Faction == Faction.OfPlayer && pawn.IsFreeColonist;
        }

        //检查地图及所有嵌套运输容器，避免乘员暂未生成到格子时误判团灭。
        internal static bool HasActive(Map map)
        {
            foreach (Pawn pawn in map.mapPawns.AllPawns)
                if (IsActive(pawn)) return true;
            return false;
        }

        //寻找已完成地图部署的作战单位，作为初始化与霸王虫首轮目标。
        internal static Pawn FindSpawned(Map map)
        {
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                if (IsActive(pawn)) return pawn;
            return null;
        }

        //优先选择地表作战单位，全部乘员在容器内时仍提供可用于准时登场的位置来源。
        internal static Pawn FindEncounterTarget(Map map)
        {
            Pawn spawned = FindSpawned(map);
            if (spawned != null) return spawned;
            foreach (Pawn pawn in map.mapPawns.AllPawns)
                if (IsActive(pawn) && pawn.MapHeld == map && pawn.PositionHeld.IsValid) return pawn;
            return null;
        }

        //通关或初始化失败后保护仍在地图和容器里的存活玩家单位及俘虏。
        internal static bool HasLivingPlayerParty(Map map)
        {
            foreach (Pawn pawn in map.mapPawns.AllPawns)
                if (!pawn.Destroyed && !pawn.Dead && (pawn.Faction == Faction.OfPlayer || pawn.IsPrisonerOfColony))
                    return true;
            return false;
        }
    }
}
