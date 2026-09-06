using UnityEngine;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //保存一次纯视觉倒塌的坐标、方向和游戏时间，不继续充当孢子源或攻击目标。
    internal sealed class SporeCollapseState : IExposable
    {
        private const int ImpactTick = 75;
        private const int EndTick = 210;
        private Vector3 position;
        private float directionAngle;
        private int startTick;
        private bool impactPlayed;
        internal Vector3 Position => position;
        private int Age => Mathf.Max(0, Find.TickManager.TicksGame - startTick);
        internal bool Finished => Age >= EndTick;
        internal float Sink => 12f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(90f, EndTick, Age));
        internal Quaternion Rotation
        {
            get
            {
                float fall = Mathf.Clamp01(Age / (float)ImpactTick);
                float angle = 82f * fall * fall;
                angle += Mathf.Sin(Age * 0.8f) * 1.5f * (1f - Mathf.Clamp01(Age / 24f));
                return Quaternion.AngleAxis(angle, Vector3.Cross(Vector3.up, Direction));
            }
        }
        private Vector3 Direction => Quaternion.AngleAxis(directionAngle, Vector3.up) * Vector3.forward;

        //供存档系统恢复倒塌记录。
        public SporeCollapseState() { }

        //记录死亡位置与随机倾倒方向，并从当前游戏刻度开始播放。
        internal SporeCollapseState(Vector3 position)
        {
            this.position = position;
            directionAngle = Rand.Range(0f, 360f);
            startTick = Find.TickManager.TicksGame;
        }

        //在虫体触地时只播放一次沿投影倒塌方向展开的原版尘雾。
        internal void Tick(Map map)
        {
            if (impactPlayed || Age < ImpactTick) return;
            impactPlayed = true;
            Vector3 projected = SporeSpewerView.ProjectToMap(Direction);
            SporeVisualEffects.CollapseImpact(map, position, projected.normalized);
        }

        //保存倒塌进度和落地反馈标记，读档不重新开始或重复扬尘。
        public void ExposeData()
        {
            Scribe_Values.Look(ref position, "position");
            Scribe_Values.Look(ref directionAngle, "directionAngle");
            Scribe_Values.Look(ref startTick, "startTick");
            Scribe_Values.Look(ref impactPlayed, "impactPlayed");
        }
    }
}
