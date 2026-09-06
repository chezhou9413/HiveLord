using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //保存一次地表冲击尘浪的形状和游戏计时，逐层向外推进纯视觉反馈。
    internal sealed class HiveLordImpactWave : IExposable
    {
        private const int DurationTicks = 42;
        private Vector3 origin;
        private Vector3 direction;
        private float length;
        private float halfWidth;
        private int elapsed;
        private bool directional;
        public bool Finished => elapsed >= DurationTicks;

        //提供存档反序列化所需的无参构造入口。
        public HiveLordImpactWave()
        {
        }

        //记录尘浪起点、方向与范围，环形波使用宽度零作为标识。
        public HiveLordImpactWave(Vector3 center, Vector3 axis, float distance, float width)
        {
            origin = center;
            direction = axis;
            length = distance;
            halfWidth = width;
            directional = width > 0f;
        }

        //按游戏时间推进波前，每六 Tick 生成一层稀疏尘浪与碎石。
        public void Tick(Map map)
        {
            elapsed++;
            if (elapsed % 6 != 0) return;
            float progress = elapsed / (float)DurationTicks;
            float distance = Mathf.Lerp(2f, length, Mathf.Sqrt(progress));
            int samples = directional ? 13 : 24;
            Vector3 side = new Vector3(-direction.z, 0f, direction.x);
            for (int index = 0; index < samples; index++)
            {
                float fraction = index / (float)(samples - 1);
                float angle = index * Mathf.PI * 2f / samples;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 position = directional
                    ? origin + direction * distance + side * Mathf.Lerp(-halfWidth, halfWidth, fraction)
                    : origin + radial * distance;
                HiveLordCombatEffects.SpawnWaveFront(map, position, directional ? direction : radial, progress, index % 4 == 0);
            }
        }

        //保存尘浪几何与进度，避免读档重复爆发或在暂停期间继续扩散。
        public void ExposeData()
        {
            Scribe_Values.Look(ref origin, "origin");
            Scribe_Values.Look(ref direction, "direction");
            Scribe_Values.Look(ref length, "length");
            Scribe_Values.Look(ref halfWidth, "halfWidth");
            Scribe_Values.Look(ref elapsed, "elapsed");
            Scribe_Values.Look(ref directional, "directional");
        }
    }
}
