using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //在地图上独立推进破土、砸地和倒地尘浪，使其不依赖虫体后续状态。
    public sealed class HiveLordImpactWaveMapComponent : MapComponent
    {
        private List<HiveLordImpactWave> waves = new List<HiveLordImpactWave>();

        //绑定原版自动创建组件时传入的地图。
        public HiveLordImpactWaveMapComponent(Map map) : base(map)
        {
        }

        //登记一个从指定中心向外扩散的环形尘浪。
        public void AddRadial(Vector3 center, float radius)
        {
            waves.Add(new HiveLordImpactWave(center, Vector3.forward, radius, 0f));
        }

        //登记沿实际砸地长条向前传播的尘浪。
        public void AddSlam(HiveLordProjectionThing owner)
        {
            Vector3 direction = (owner.CombatAimCell - owner.Position).ToVector3().normalized;
            waves.Add(new HiveLordImpactWave(owner.DrawPos, direction,
                owner.CombatSettings.slamAreaForwardLength, owner.CombatSettings.slamAreaHalfWidth));
        }

        //推进所有活动尘浪并移除完成的短时效果。
        public override void MapComponentTick()
        {
            for (int index = waves.Count - 1; index >= 0; index--)
            {
                waves[index].Tick(map);
                if (waves[index].Finished) waves.RemoveAt(index);
            }
        }

        //保存活动尘浪列表，使视觉推进随存档恢复。
        public override void ExposeData()
        {
            Scribe_Collections.Look(ref waves, "hiveLordImpactWaves", LookMode.Deep);
        }
    }
}
