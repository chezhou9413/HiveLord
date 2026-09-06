using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //根据霸王虫位置与瞄准方向生成吐酸梯形和砸地长条的实际地图格区域。
    internal static class HiveLordAttackArea
    {
        //生成从虫体前方向外扩张的吐酸区域，使远端酸液覆盖面随距离变宽。
        public static void FillAcidArea(
            HiveLordProjectionThing owner,
            IntVec3 aimCell,
            HiveLordCombatExtension settings,
            List<IntVec3> cells)
        {
            FillDirectionalArea(
                owner.Map,
                owner.Position,
                aimCell,
                settings.acidAreaStartOffset,
                settings.acidAreaLength,
                settings.acidAreaNearHalfWidth,
                settings.acidAreaFarHalfWidth,
                cells);
        }

        //生成贯穿虫体前后并沿攻击方向延伸的砸地长条区域。
        public static void FillGroundSlamArea(
            HiveLordProjectionThing owner,
            IntVec3 aimCell,
            HiveLordCombatExtension settings,
            List<IntVec3> cells)
        {
            FillDirectionalArea(
                owner.Map,
                owner.Position,
                aimCell,
                -settings.slamAreaBackLength,
                settings.slamAreaForwardLength,
                settings.slamAreaHalfWidth,
                settings.slamAreaHalfWidth,
                cells);
        }

        //枚举方向坐标系包围盒，并按前向距离插值宽度筛出梯形或长条内的地图格。
        private static void FillDirectionalArea(
            Map map,
            IntVec3 origin,
            IntVec3 aimCell,
            float startDistance,
            float endDistance,
            float startHalfWidth,
            float endHalfWidth,
            List<IntVec3> cells)
        {
            cells.Clear();
            Vector2 forward = ResolveForward(origin, aimCell);
            Vector2 lateral = new Vector2(-forward.y, forward.x);
            float maximumHalfWidth = Mathf.Max(startHalfWidth, endHalfWidth);
            int bounds = Mathf.CeilToInt(
                Mathf.Max(Mathf.Abs(startDistance), Mathf.Abs(endDistance))
                + maximumHalfWidth
                + 1f);

            for (int x = -bounds; x <= bounds; x++)
            {
                for (int z = -bounds; z <= bounds; z++)
                {
                    IntVec3 cell = new IntVec3(origin.x + x, 0, origin.z + z);
                    if (!cell.InBounds(map))
                    {
                        continue;
                    }

                    Vector2 offset = new Vector2(x, z);
                    float forwardDistance = Vector2.Dot(offset, forward);
                    if (forwardDistance < startDistance || forwardDistance > endDistance)
                    {
                        continue;
                    }

                    float progress = Mathf.InverseLerp(startDistance, endDistance, forwardDistance);
                    float permittedHalfWidth = Mathf.Lerp(startHalfWidth, endHalfWidth, progress);
                    if (Mathf.Abs(Vector2.Dot(offset, lateral)) <= permittedHalfWidth)
                    {
                        cells.Add(cell);
                    }
                }
            }
        }

        //把瞄准格转换为单位前向；目标与虫体重合时使用地图北向作为稳定方向。
        private static Vector2 ResolveForward(IntVec3 origin, IntVec3 aimCell)
        {
            Vector2 direction = new Vector2(aimCell.x - origin.x, aimCell.z - origin.z);
            return direction.sqrMagnitude <= 0.0001f ? Vector2.up : direction.normalized;
        }
    }
}
