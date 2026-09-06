using RimWorld;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //绘制扩散尘浪、死亡倒地与尸体下沉的地表反馈。
    internal static partial class HiveLordCombatEffects
    {
        //为波前生成向外运动的低层尘浪，间隔配以地裂及抛射碎石。
        internal static void SpawnWaveFront(Map map, Vector3 position, Vector3 direction, float progress, bool debris)
        {
            if (!position.ToIntVec3().InBounds(map) || !position.ShouldSpawnMotesAt(map, false)) return;
            FleckCreationData dust = FleckMaker.GetDataStatic(position, map, CraterDustThick, Mathf.Lerp(5.5f, 3f, progress));
            dust.velocityAngle = direction.AngleFlat();
            dust.velocitySpeed = Mathf.Lerp(10f, 3f, progress);
            dust.rotationRate = Rand.Range(-90f, 90f);
            dust.instanceColor = new Color(0.62f, 0.49f, 0.29f, 0.58f);
            dust.solidTimeOverride = 0.15f;
            map.flecks.CreateFleck(dust);
            if (!debris) return;
            FleckMaker.Static(position, map, GroundCrack, Rand.Range(3f, 5f));
            SpawnThrownDebris(map, position, 0.6f, Rand.Range(6f, 12f), Rand.Range(1.2f, 2.8f));
        }

        //死亡时生成短促冲击和扩散尘环，只表现尸体重量而不造成攻击伤害。
        public static void SpawnDeathImpact(HiveLordProjectionThing owner)
        {
            Vector3 center = owner.Position.ToVector3Shifted();
            owner.Map.GetComponent<HiveLordImpactWaveMapComponent>().AddRadial(center, 20f * owner.SizeFactor);
            if (!center.ShouldSpawnMotesAt(owner.Map, false)) return;
            FleckMaker.Static(center, owner.Map, ShockwaveFast, 18f * owner.SizeFactor);
            FleckMaker.Static(center, owner.Map, GroundCrack, 20f * owner.SizeFactor);
            for (int index = 0; index < 24; index++)
            {
                SpawnThrownDebris(owner.Map, center, Rand.Range(3f, 12f) * owner.SizeFactor,
                    Rand.Range(5f, 11f) * owner.SizeFactor, Rand.Range(2f, 4f) * owner.SizeFactor);
            }
            if (Find.CurrentMap == owner.Map && Find.CameraDriver != null)
            {
                Find.CameraDriver.shaker.DoShake(0.16f);
            }
        }

        //尸体下沉时在虫体周边生成逐渐减弱并向中心流动的尘土。
        public static void TickDeathDust(HiveLordProjectionThing owner)
        {
            Vector3 center = owner.Position.ToVector3Shifted();
            float progress = owner.DeathSinkProgress;
            for (int index = 0; index < 5; index++)
            {
                Vector3 offset = Gen.RandomHorizontalVector(Mathf.Lerp(12f, 4f, progress) * owner.SizeFactor);
                Vector3 location = center + offset;
                if (!location.ToIntVec3().InBounds(owner.Map) || !location.ShouldSpawnMotesAt(owner.Map, false)) continue;
                FleckCreationData dust = FleckMaker.GetDataStatic(location, owner.Map, CraterDustThick,
                    Mathf.Lerp(4f, 1.5f, progress) * owner.SizeFactor);
                dust.velocityAngle = (-offset).AngleFlat();
                dust.velocitySpeed = 1.5f;
                dust.instanceColor = new Color(0.64f, 0.51f, 0.32f, 0.5f * (1f - progress));
                dust.solidTimeOverride = 0.2f;
                owner.Map.flecks.CreateFleck(dust);
            }
        }
    }
}
