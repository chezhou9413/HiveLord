using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //绘制霸王虫攻击区域，并在实际命中格生成酸蚀或砸地反馈。
    internal static partial class HiveLordCombatEffects
    {
        //用绿色边缘显示吐酸梯形，命中后叠加斜线提供地面受击反馈。
        public static void DrawAcidDamageArea(List<IntVec3> cells, bool impacted)
        {
            if (cells.Count == 0)
            {
                return;
            }

            GenDraw.DrawFieldEdges(cells, AcidAreaColor);
            if (impacted)
            {
                GenDraw.DrawDiagonalStripes(cells, new Color(0.32f, 0.7f, 0.08f, 0.22f));
            }
        }

        //用红橙边缘显示砸地长条，命中后叠加斜线提供地面受击反馈。
        public static void DrawSlamDamageArea(List<IntVec3> cells, bool impacted)
        {
            if (cells.Count == 0)
            {
                return;
            }

            GenDraw.DrawFieldEdges(cells, SlamAreaColor);
            if (impacted)
            {
                GenDraw.DrawDiagonalStripes(cells, new Color(0.85f, 0.16f, 0.04f, 0.2f));
            }
        }

        //在全部可见吐酸格中密集散布酸液飞溅与黄绿色腐蚀云。
        public static void SpawnAcidImpact(Map map, IReadOnlyList<IntVec3> cells)
        {
            int splashCount = Mathf.Clamp(cells.Count / 4, 120, 320);
            for (int index = 0; index < splashCount; index++)
            {
                Vector3 location = RandomCellPosition(cells);
                FleckCreationData splash = FleckMaker.GetDataStatic(
                    location,
                    map,
                    AcidSpray,
                    Rand.Range(3f, 6.5f));
                splash.velocityAngle = Rand.Range(0f, 360f);
                splash.velocitySpeed = Rand.Range(1.4f, 3.4f);
                splash.rotationRate = Rand.Range(-80f, 80f);
                splash.solidTimeOverride = Rand.Range(1f, 2.4f);
                splash.instanceColor = Color.Lerp(AcidCloudColor, new Color(0.78f, 0.94f, 0.18f, 0.8f), Rand.Value);
                map.flecks.CreateFleck(splash);
                if (index % 4 == 0)
                {
                    FleckCreationData cloud = FleckMaker.GetDataStatic(
                        location,
                        map,
                        CraterDustThick,
                        Rand.Range(3.2f, 6.8f));
                    cloud.instanceColor = new Color(0.44f, 0.64f, 0.08f, 0.45f);
                    cloud.rotationRate = Rand.Range(-60f, 60f);
                    cloud.velocityAngle = Rand.Range(0f, 360f);
                    cloud.velocitySpeed = Rand.Range(0.25f, 0.8f);
                    map.flecks.CreateFleck(cloud);
                }
            }
        }

        //在吐酸命中后的残留期持续补充贴地酸雾，使腐蚀区域保持十秒以上的动态反馈。
        public static void TickAcidResidue(Map map, IReadOnlyList<IntVec3> cells)
        {
            if (map == null || cells == null || cells.Count == 0)
            {
                return;
            }

            int fleckCount = Mathf.Clamp(cells.Count / 180, 6, 18);
            for (int index = 0; index < fleckCount; index++)
            {
                Vector3 location = RandomCellPosition(cells);
                FleckCreationData residue = FleckMaker.GetDataStatic(
                    location,
                    map,
                    AcidSpray,
                    Rand.Range(2.4f, 4.8f));
                residue.velocityAngle = Rand.Range(0f, 360f);
                residue.velocitySpeed = Rand.Range(0.08f, 0.35f);
                residue.rotationRate = Rand.Range(-35f, 35f);
                residue.solidTimeOverride = Rand.Range(2.5f, 4.2f);
                residue.instanceColor = new Color(0.46f, 0.72f, 0.1f, 0.58f);
                map.flecks.CreateFleck(residue);
            }
        }

        //沿完整砸地长条分散生成冲击波、巨大地裂和连续扬尘。
        public static void SpawnGroundSlamImpact(Map map, IReadOnlyList<IntVec3> cells)
        {
            int effectCount = Mathf.Clamp(cells.Count / 12, 48, 120);
            for (int index = 0; index < effectCount; index++)
            {
                Vector3 location = RandomCellPosition(cells);
                if (index % 3 == 0)
                {
                    FleckMaker.Static(location, map, GroundCrack, Rand.Range(6f, 13f));
                }

                if (index % 12 == 0)
                {
                    FleckMaker.Static(location, map, ShockwaveFast, Rand.Range(2.5f, 5f));
                }

                SpawnThrownDebris(map, location, 0.8f, Rand.Range(4f, 9f), Rand.Range(1.5f, 3.5f));
                if (index % 2 == 0)
                {
                    FleckMaker.ThrowDustPuffThick(location, map, Rand.Range(2.5f, 5f), DirtCloudColor);
                }
            }
        }

        //从攻击格集合随机选择一个格心并加入轻微偏移，避免 Fleck 排列成规则网格。
        private static Vector3 RandomCellPosition(IReadOnlyList<IntVec3> cells)
        {
            IntVec3 cell = cells[Rand.Range(0, cells.Count)];
            return cell.ToVector3Shifted() + Gen.RandomHorizontalVector(Rand.Range(0f, 0.45f));
        }
    }
}
