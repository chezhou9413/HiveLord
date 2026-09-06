using RimWorld;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //复用原版地裂、碎石、酸液、冲击波和扬尘 Fleck，表现钻地过程与攻击范围。
    internal static partial class HiveLordCombatEffects
    {
        private const int EmergenceCrackCount = 14;
        private const int EmergenceMajorCrackCount = 7;
        private const int EmergenceDebrisCount = 96;
        private const int EmergenceSustainIntervalTicks = 2;
        private const int EmergenceSustainDebrisCount = 5;
        private const int EmergenceSustainCloudCount = 4;
        private const int SubmergingDebrisIntervalTicks = 2;
        private const int SubmergingDebrisCount = 12;
        private static readonly Color AcidAreaColor = new Color(0.48f, 0.82f, 0.12f, 0.9f);
        private static readonly Color SlamAreaColor = new Color(0.92f, 0.27f, 0.08f, 0.9f);
        private static readonly Color AcidCloudColor = new Color(0.52f, 0.72f, 0.08f, 0.9f);
        private static readonly Color DirtCloudColor = new Color(0.72f, 0.55f, 0.24f, 0.94f);
        private static FleckDef groundCrack;
        private static FleckDef thrownDebris;
        private static FleckDef craterDustThick;
        private static FleckDef acidSpray;
        private static FleckDef shockwaveFast;

        //在地下预警开始时叠出类似原版巨坑的大型裂地，并铺开外围裂纹与泥尘。
        public static void SpawnEmergenceWarning(HiveLordProjectionThing owner)
        {
            Vector3 center = owner.Position.ToVector3Shifted();
            for (int index = 0; index < EmergenceMajorCrackCount; index++)
            {
                FleckCreationData majorCrack = FleckMaker.GetDataStatic(
                    center + Gen.RandomHorizontalVector(Rand.Range(0f, 3.5f) * owner.SizeFactor),
                    owner.Map,
                    GroundCrack,
                    Rand.Range(22f, 34f) * owner.SizeFactor);
                majorCrack.rotation = Rand.Range(0f, 360f);
                owner.Map.flecks.CreateFleck(majorCrack);
            }

            for (int index = 0; index < EmergenceCrackCount; index++)
            {
                Vector3 location = center + Gen.RandomHorizontalVector(Rand.Range(3f, 18f) * owner.SizeFactor);
                FleckMaker.Static(location, owner.Map, GroundCrack, Rand.Range(8f, 16f) * owner.SizeFactor);
                SpawnSoilDustPuff(location, owner.Map, Rand.Range(5f, 9f) * owner.SizeFactor);
            }
        }

        //在地下换位预警期间持续从巨型裂地周围冒出原版巨坑泥尘。
        public static void TickEmergenceWarning(HiveLordProjectionThing owner, int remainingTicks)
        {
            if (remainingTicks % 3 != 0)
            {
                return;
            }

            Vector3 center = owner.Position.ToVector3Shifted();
            for (int index = 0; index < 3; index++)
            {
                Vector3 location = center + Gen.RandomHorizontalVector(Rand.Range(1f, 20f) * owner.SizeFactor);
                SpawnSoilSmoke(location, owner.Map, Rand.Range(3.5f, 6.5f) * owner.SizeFactor);
                SpawnSoilDustPuff(location, owner.Map, Rand.Range(5.5f, 10f) * owner.SizeFactor);
                FleckCreationData craterDust = FleckMaker.GetDataStatic(
                    location,
                    owner.Map,
                    CraterDustThick,
                    Rand.Range(4f, 8f) * owner.SizeFactor);
                craterDust.rotationRate = Rand.Range(-90f, 90f);
                craterDust.velocityAngle = Rand.Range(0f, 360f);
                craterDust.velocitySpeed = Rand.Range(0.8f, 1.8f);
                craterDust.instanceColor = DirtCloudColor;
                owner.Map.flecks.CreateFleck(craterDust);
            }
        }

        //出土动画开始时向四周抛出大量原版碎石并播放集中扬尘。
        public static void SpawnEmergenceBurst(HiveLordProjectionThing owner)
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

            for (int index = 0; index < EmergenceDebrisCount; index++)
            {
                SpawnThrownDebris(
                    owner.Map,
                    center,
                    Rand.Range(1.5f, 20f) * owner.SizeFactor,
                    Rand.Range(7f, 15f) * owner.SizeFactor,
                    Rand.Range(3.5f, 7.5f) * owner.SizeFactor);
            }

            for (int index = 0; index < 40; index++)
            {
                Vector3 location = center + Gen.RandomHorizontalVector(Rand.Range(0.5f, 16f) * owner.SizeFactor);
                SpawnSoilSmoke(location, owner.Map, Rand.Range(3.5f, 7f) * owner.SizeFactor);
                SpawnSoilDustPuff(location, owner.Map, Rand.Range(6f, 12f) * owner.SizeFactor);
            }
        }

        //出土动画期间持续补充大块碎石、浓烟和厚尘，让地表喷发延续到虫体完全出现。
        public static void TickEmergenceDebris(HiveLordProjectionThing owner)
        {
            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame % EmergenceSustainIntervalTicks != 0 || !owner.Spawned)
            {
                return;
            }

            Vector3 center = owner.Position.ToVector3Shifted();
            if (!center.ShouldSpawnMotesAt(owner.Map, false))
            {
                return;
            }

            for (int index = 0; index < EmergenceSustainDebrisCount; index++)
            {
                SpawnThrownDebris(
                    owner.Map,
                    center,
                    Rand.Range(1f, 18f) * owner.SizeFactor,
                    Rand.Range(6f, 13f) * owner.SizeFactor,
                    Rand.Range(3.2f, 7f) * owner.SizeFactor);
            }

            for (int index = 0; index < EmergenceSustainCloudCount; index++)
            {
                Vector3 location = center + Gen.RandomHorizontalVector(Rand.Range(1f, 20f) * owner.SizeFactor);
                SpawnSoilSmoke(location, owner.Map, Rand.Range(3.2f, 6.5f) * owner.SizeFactor);
                SpawnSoilDustPuff(location, owner.Map, Rand.Range(5.5f, 11f) * owner.SizeFactor);
            }
        }

        //入土动画开始时立即喷出首批碎石，使虫体下沉与地面崩落同步起势。
        public static void SpawnSubmergingBurst(HiveLordProjectionThing owner)
        {
            SpawnSubmergingDebris(owner, 20);
            HiveLordSubmergingClouds.SpawnBurst(owner);
        }

        //入土动画期间按独立频率持续抛出碎石，并在大片地表生成烟雾、厚尘与巨坑扬尘。
        public static void TickSubmergingDebris(HiveLordProjectionThing owner)
        {
            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame % SubmergingDebrisIntervalTicks == 0)
            {
                SpawnSubmergingDebris(owner, SubmergingDebrisCount);
            }

            HiveLordSubmergingClouds.Tick(owner, ticksGame);
        }

        //在镜头附近创建一批入土碎石，并用两团厚泥尘填充石块之间的空隙。
        private static void SpawnSubmergingDebris(HiveLordProjectionThing owner, int count)
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
                SpawnThrownDebris(
                    owner.Map,
                    center,
                    Rand.Range(1f, 12f) * owner.SizeFactor,
                    Rand.Range(3.5f, 8.5f) * owner.SizeFactor,
                    Rand.Range(1.8f, 4.5f) * owner.SizeFactor);
            }

            for (int index = 0; index < 2; index++)
            {
                Vector3 dustPosition = center + Gen.RandomHorizontalVector(Rand.Range(0.5f, 5.8f) * owner.SizeFactor);
                SpawnSoilDustPuff(dustPosition, owner.Map, Rand.Range(2.4f, 4.5f) * owner.SizeFactor);
            }
        }

        //按原版烟雾运动参数生成土黄色烟团，替代无法着色的默认灰烟接口。
        internal static void SpawnSoilSmoke(Vector3 location, Map map, float size)
        {
            if (!location.ShouldSpawnMotesAt(map))
            {
                return;
            }

            FleckCreationData smoke = FleckMaker.GetDataStatic(
                location,
                map,
                FleckDefOf.Smoke,
                Rand.Range(1.5f, 2.5f) * size);
            smoke.rotationRate = Rand.Range(-30f, 30f);
            smoke.velocityAngle = Rand.Range(30f, 40f);
            smoke.velocitySpeed = Rand.Range(0.5f, 0.7f);
            smoke.instanceColor = DirtCloudColor;
            map.flecks.CreateFleck(smoke);
        }

        //生成带实例颜色的土黄色厚尘，使地表烟尘与钻地土壤色调一致。
        internal static void SpawnSoilDustPuff(Vector3 location, Map map, float scale)
        {
            if (!location.ShouldSpawnMotesAt(map))
            {
                return;
            }

            FleckCreationData dust = FleckMaker.GetDataStatic(
                location,
                map,
                FleckDefOf.DustPuffThick,
                scale);
            dust.rotationRate = Rand.Range(-60f, 60f);
            dust.velocityAngle = Rand.Range(0f, 360f);
            dust.velocitySpeed = Rand.Range(0.6f, 0.75f);
            dust.instanceColor = DirtCloudColor;
            map.flecks.CreateFleck(dust);
        }

        //从中心周围选取地表起点，创建带抛物线、旋转和径向速度的原版碎石 Fleck。
        private static void SpawnThrownDebris(
            Map map,
            Vector3 center,
            float spawnRadius,
            float speed,
            float scale)
        {
            Vector3 radialOffset = Gen.RandomHorizontalVector(spawnRadius);
            FleckCreationData debris = FleckMaker.GetDataStatic(
                center + radialOffset,
                map,
                ThrownDebris,
                scale);
            debris.rotation = Rand.Range(0f, 360f);
            debris.rotationRate = Rand.Range(-480f, 480f);
            debris.velocityAngle = radialOffset.AngleFlat() + Rand.Range(-16f, 16f);
            debris.velocitySpeed = speed;
            debris.airTimeLeft = Rand.Range(0.9f, 1.2f);
            map.flecks.CreateFleck(debris);
        }

        private static FleckDef GroundCrack => groundCrack
            ?? (groundCrack = DefDatabase<FleckDef>.GetNamed("GroundCrack"));
        private static FleckDef ThrownDebris => thrownDebris
            ?? (thrownDebris = DefDatabase<FleckDef>.GetNamed("ThrownDebris"));
        private static FleckDef CraterDustThick => craterDustThick
            ?? (craterDustThick = DefDatabase<FleckDef>.GetNamed("CraterFilledInDustThick"));
        private static FleckDef AcidSpray => acidSpray
            ?? (acidSpray = DefDatabase<FleckDef>.GetNamed("AcidSpray"));
        private static FleckDef ShockwaveFast => shockwaveFast
            ?? (shockwaveFast = DefDatabase<FleckDef>.GetNamed("ShockwaveFast"));
    }
}
