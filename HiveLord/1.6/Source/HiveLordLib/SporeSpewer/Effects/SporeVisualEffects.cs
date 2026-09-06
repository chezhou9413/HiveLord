using RimWorld;
using UnityEngine;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //通过地图 Fleck 表现各孢子源喷涌、稀疏漂浮颗粒和死亡爆散。
    internal static class SporeVisualEffects
    {
        private static readonly Color Orange = new Color(1f, 0.43f, 0.07f, 0.65f);

        //在虫体周围喷出原版间歇泉气雾，并在顶部投影位置产生橙色孢子云。
        internal static void EmitSource(Map map, Vector3 center)
        {
            if (!center.ShouldSpawnMotesAt(map, false)) return;
            EmitGroundMist(map, center);
            //顶部孢子与虫体共用镜头投影，避免调整俯角后喷出位置偏离结节。
            center += SporeSpewerView.SporeOutletOffset;
            float scale = SporeSpewerView.RenderScale;
            for (int i = 0; i < 3; i++)
                Cloud(map, center + Gen.RandomHorizontalVector(0.8f * scale),
                    Rand.Range(1.5f, 2.5f) * scale, Rand.Range(0.6f, 1.4f) * scale);
        }

        //用原版间歇泉的气雾粒子在根部外围形成少量向外散开的浅橙喷雾。
        private static void EmitGroundMist(Map map, Vector3 center)
        {
            for (int i = 0; i < 2; i++)
            {
                float angle = Rand.Range(0f, 360f);
                Vector3 offset = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward
                    * Rand.Range(2.7f, 3.8f);
                Vector3 position = center + offset;
                if (!position.InBounds(map) || !position.ShouldSpawnMotesAt(map, false)) continue;
                FleckCreationData data = FleckMaker.GetDataStatic(position, map, FleckDefOf.AirPuff,
                    Rand.Range(1.2f, 1.8f));
                data.instanceColor = new Color(1f, 0.73f, 0.38f, 0.65f);
                data.velocityAngle = angle;
                data.velocitySpeed = Rand.Range(0.6f, 1.1f);
                data.rotationRate = Rand.Range(-90f, 90f);
                map.flecks.CreateFleck(data);
            }
        }

        //沿倒塌方向扬起落地尘雾和碎石，只产生视觉效果。
        internal static void CollapseImpact(Map map, Vector3 center, Vector3 direction)
        {
            if (map != Find.CurrentMap || !center.ShouldSpawnMotesAt(map, false)) return;
            Burst(map, center + direction * 2f);
            for (int i = 0; i < 18; i++)
            {
                Vector3 position = center + direction * Rand.Range(0f, 4.5f)
                    + Gen.RandomHorizontalVector(2f);
                if (!position.InBounds(map)) continue;
                FleckCreationData data = FleckMaker.GetDataStatic(position, map, FleckDefOf.DustPuffThick,
                    Rand.Range(2f, 3.5f));
                data.instanceColor = new Color(0.68f, 0.43f, 0.2f, 0.8f);
                data.velocityAngle = Rand.Range(0f, 360f);
                data.velocitySpeed = Rand.Range(0.8f, 2f);
                data.rotationRate = Rand.Range(-45f, 45f);
                map.flecks.CreateFleck(data);
            }
        }

        //在当前可见地图区域生成少量漂浮孢子，数量不随孢子源叠加。
        internal static void EmitMapSpores(Map map, float density)
        {
            CellRect view = Find.CameraDriver.CurrentViewRect;
            for (int i = 0; i < 3; i++)
            {
                IntVec3 cell = view.RandomCell;
                if (!cell.InBounds(map) || Rand.Value > density) continue;
                FleckCreationData data = FleckMaker.GetDataStatic(cell.ToVector3Shifted(), map, FleckDefOf.MicroSparks, 0.35f);
                data.instanceColor = Orange;
                data.velocityAngle = 55f;
                data.velocitySpeed = 0.5f;
                map.flecks.CreateFleck(data);
            }
        }

        //在虫体消失处喷散孢子云与短时碎屑，不生成伤害或战利品。
        internal static void Burst(Map map, Vector3 center)
        {
            if (map != Find.CurrentMap || !center.ShouldSpawnMotesAt(map, false)) return;
            for (int i = 0; i < 24; i++)
                Cloud(map, center + Gen.RandomHorizontalVector(3f), Rand.Range(2f, 4f), Rand.Range(3f, 7f));
            for (int i = 0; i < 12; i++)
            {
                FleckCreationData data = FleckMaker.GetDataStatic(center, map, DefDatabase<FleckDef>.GetNamed("ThrownDebris"), Rand.Range(0.25f, 0.6f));
                data.velocityAngle = Rand.Range(0f, 360f);
                data.velocitySpeed = Rand.Range(3f, 8f);
                data.rotationRate = Rand.Range(-180f, 180f);
                data.airTimeLeft = Rand.Range(0.5f, 1.2f);
                data.instanceColor = new Color(0.45f, 0.26f, 0.08f);
                map.flecks.CreateFleck(data);
            }
        }

        //创建由游戏刻度驱动的橙色烟云粒子。
        private static void Cloud(Map map, Vector3 position, float scale, float speed)
        {
            FleckCreationData data = FleckMaker.GetDataStatic(position, map, FleckDefOf.Smoke, scale);
            data.instanceColor = Orange;
            data.velocityAngle = Rand.Range(0f, 360f);
            data.velocitySpeed = speed;
            data.rotationRate = Rand.Range(-15f, 15f);
            data.solidTimeOverride = 0.4f;
            map.flecks.CreateFleck(data);
        }
    }
}
