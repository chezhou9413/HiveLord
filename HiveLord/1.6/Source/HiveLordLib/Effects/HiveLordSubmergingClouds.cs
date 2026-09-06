using RimWorld;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //在霸王虫钻地阶段持续铺开大范围原版烟雾、厚尘与贴地巨坑扬尘。
    internal static class HiveLordSubmergingClouds
    {
        private const int CloudIntervalTicks = 2;
        private const int SustainedCloudCount = 5;
        private const int InitialCloudCount = 28;
        private static readonly Color DirtCloudColor = new Color(0.72f, 0.55f, 0.24f, 0.94f);
        private static FleckDef craterDustThick;

        //钻地动画开始时立即铺开首批覆盖十八格范围的大片烟尘。
        public static void SpawnBurst(HiveLordProjectionThing owner)
        {
            Spawn(owner, InitialCloudCount);
        }

        //钻地动画期间每两 Tick 补充五组烟雾、厚尘与贴地尘浪。
        public static void Tick(HiveLordProjectionThing owner, int ticksGame)
        {
            if (ticksGame % CloudIntervalTicks == 0)
            {
                Spawn(owner, SustainedCloudCount);
            }
        }

        //在虫体周围随机地表位置组合三种原版 Fleck，形成宽广且有高度层次的尘云。
        private static void Spawn(HiveLordProjectionThing owner, int count)
        {
            if (!owner.Spawned)
            {
                return;
            }

            Vector3 center = owner.Position.ToVector3Shifted();
            if (!center.ShouldSpawnMotesAt(owner.Map, false))
            {
                return;
            }

            for (int index = 0; index < count; index++)
            {
                Vector3 location = center + Gen.RandomHorizontalVector(Rand.Range(1f, 18f) * owner.SizeFactor);
                HiveLordCombatEffects.SpawnSoilSmoke(
                    location,
                    owner.Map,
                    Rand.Range(3f, 5.5f) * owner.SizeFactor);
                HiveLordCombatEffects.SpawnSoilDustPuff(
                    location,
                    owner.Map,
                    Rand.Range(5.5f, 10f) * owner.SizeFactor);
                FleckCreationData groundDust = FleckMaker.GetDataStatic(
                    location,
                    owner.Map,
                    CraterDustThick,
                    Rand.Range(4f, 8f) * owner.SizeFactor);
                groundDust.rotationRate = Rand.Range(-75f, 75f);
                groundDust.velocityAngle = Rand.Range(0f, 360f);
                groundDust.velocitySpeed = Rand.Range(0.35f, 0.9f);
                groundDust.instanceColor = DirtCloudColor;
                owner.Map.flecks.CreateFleck(groundDust);
            }
        }

        private static FleckDef CraterDustThick => craterDustThick
            ?? (craterDustThick = DefDatabase<FleckDef>.GetNamed("CraterFilledInDustThick"));
    }
}
