using Verse;

namespace HiveLordLib
{
    //从目标周围的配置环带中随机选择一个可供霸王虫出土的地图格。
    internal static class HiveLordEmergencePlanner
    {
        //围绕当前地表目标的位置选择出土环带。
        public static bool TryChooseCell(
            HiveLordProjectionThing owner,
            Thing target,
            HiveLordPlannedAttack attack,
            HiveLordCombatExtension settings,
            out IntVec3 result)
        {
            return TryChooseCell(owner.Map, target.Position, attack, settings, out result, owner.HitProxyDef);
        }

        //围绕指定地图位置按攻击半径抽样，支持运输容器中的挑战目标。
        internal static bool TryChooseCell(Map map, IntVec3 targetPosition, HiveLordPlannedAttack attack,
            HiveLordCombatExtension settings, out IntVec3 result, ThingDef proxyDef = null)
        {
            float minimumRadius = attack == HiveLordPlannedAttack.Acid
                ? settings.acidEmergenceMinRadius
                : settings.slamEmergenceMinRadius;
            float maximumRadius = attack == HiveLordPlannedAttack.Acid
                ? settings.acidEmergenceMaxRadius
                : settings.slamEmergenceMaxRadius;

            result = IntVec3.Invalid;
            int validCellCount = 0;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(
                         targetPosition,
                         minimumRadius,
                         maximumRadius))
            {
                if (!IsLegalCell(cell, map, proxyDef ?? HiveLordDefOf.HiveLord_HitProxy))
                {
                    continue;
                }

                validCellCount++;
                if (Rand.RangeInclusive(1, validCellCount) == 1)
                {
                    result = cell;
                }
            }

            return result.IsValid;
        }

        //拒绝越界、不可行走、被实体建筑占据或放不下完整受击代理的出土位置。
        private static bool IsLegalCell(IntVec3 cell, Map map, ThingDef proxyDef)
        {
            return cell.InBounds(map)
                && cell.Walkable(map)
                && cell.GetEdifice(map) == null
                && CellRect.CenteredOn(cell, proxyDef.size).InBounds(map);
        }
    }
}
