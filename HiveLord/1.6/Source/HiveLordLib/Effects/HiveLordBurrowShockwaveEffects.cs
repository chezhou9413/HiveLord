using RimWorld;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //在霸王虫破土关键帧生成大尺寸环形冲击波、远射碎石和覆盖广域的浓厚尘浪。
    internal static partial class HiveLordCombatEffects
    {
        //组合三层原版冲击波与径向地表喷发，并在镜头可见时追加一次强震。
        public static void SpawnEmergenceShockwave(
            HiveLordProjectionThing owner,
            HiveLordCombatExtension settings)
        {
            if (!owner.Spawned)
            {
                return;
            }

            Map map = owner.Map;
            Vector3 center = owner.Position.ToVector3Shifted();
            map.GetComponent<HiveLordImpactWaveMapComponent>().AddRadial(center, settings.emergencePushRadius + 4f * owner.SizeFactor);
            if (!center.ShouldSpawnMotesAt(map, false))
            {
                return;
            }

            float baseScale = settings.emergenceShockwaveScale;
            for (int index = 0; index < 3; index++)
            {
                FleckCreationData shockwave = FleckMaker.GetDataStatic(
                    center,
                    map,
                    ShockwaveFast,
                    baseScale * (0.72f + index * 0.24f));
                shockwave.rotation = Rand.Range(0f, 360f);
                map.flecks.CreateFleck(shockwave);
            }

            for (int index = 0; index < 72; index++)
            {
                SpawnThrownDebris(
                    map,
                    center,
                    Rand.Range(2f, 22f) * owner.SizeFactor,
                    Rand.Range(11f, 22f) * owner.SizeFactor,
                    Rand.Range(4.5f, 9f) * owner.SizeFactor);
            }

            for (int index = 0; index < 26; index++)
            {
                Vector3 location = center + Gen.RandomHorizontalVector(Rand.Range(2f, 25f) * owner.SizeFactor);
                SpawnSoilSmoke(location, map, Rand.Range(4f, 8f) * owner.SizeFactor);
                SpawnSoilDustPuff(location, map, Rand.Range(7f, 14f) * owner.SizeFactor);
                if (index % 3 == 0)
                {
                    FleckCreationData groundDust = FleckMaker.GetDataStatic(
                        location,
                        map,
                        CraterDustThick,
                        Rand.Range(6f, 12f) * owner.SizeFactor);
                    groundDust.velocityAngle = (location - center).AngleFlat();
                    groundDust.velocitySpeed = Rand.Range(1.2f, 2.8f);
                    groundDust.rotationRate = Rand.Range(-110f, 110f);
                    groundDust.instanceColor = DirtCloudColor;
                    map.flecks.CreateFleck(groundDust);
                }
            }

            Find.CameraDriver.shaker.DoShake(settings.emergenceShakeMagnitude);
        }
    }
}
