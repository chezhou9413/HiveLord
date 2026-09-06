using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //管理霸王虫临终出土、倒伏、定格与下沉，所有阶段按游戏刻度存档。
    public sealed partial class HiveLordProjectionThing
    {
        internal const float ModelSinkDepth = 260f;
        internal const int DeathBlendTicks = 18;
        private const int DeathHoldTicks = 90;
        private const int DeathSinkTicks = 600;
        private bool isDying;
        private int deathElapsedTicks;
        private float deathStartOffset;
        private HiveLordVisualState deathSourceState;
        private float deathSourceProgress;
        private float deathEmergenceStart;
        private int deathEmergenceTicks;
        private int deathSlamTicks;
        private bool deathImpactPlayed;

        public bool IsDying => isDying;
        internal int DeathElapsedTicks => deathElapsedTicks;
        internal int DeathEmergenceTicks => deathEmergenceTicks;
        internal HiveLordVisualState DeathSourceState => deathSourceState;
        internal float DeathSourceProgress => deathSourceProgress;
        internal bool DeathEmerging => deathElapsedTicks < deathEmergenceTicks;
        private int DeathImpactTick => deathEmergenceTicks + deathSlamTicks;
        internal float DeathEmergenceProgress => Mathf.Lerp(deathEmergenceStart, 1f,
            Mathf.Clamp01(deathElapsedTicks / (float)Mathf.Max(1, deathEmergenceTicks)));
        internal float DeathSlamProgress => CombatSettings.slamImpactNormalizedTime
            * Mathf.Clamp01((deathElapsedTicks - deathEmergenceTicks) / (float)Mathf.Max(1, deathSlamTicks));
        internal float DeathSinkProgress => Mathf.Clamp01(
            (deathElapsedTicks - DeathImpactTick - DeathHoldTicks) / (float)DeathSinkTicks);

        //临终出土从实际埋深升至地面，触地定格结束后再从地面缓缓下沉。
        internal float DeathVerticalOffset => DeathEmerging
            ? Mathf.Lerp(deathStartOffset, 0f, Mathf.SmoothStep(0f, 1f,
                deathElapsedTicks / (float)Mathf.Max(1, deathEmergenceTicks)))
            : Mathf.Lerp(0f, -ModelSinkDepth, Mathf.SmoothStep(0f, 1f, DeathSinkProgress));

        //把直接击杀锚点的调用转入同一死亡流程。
        public override void Kill(DamageInfo? dinfo = null, Hediff exactCulprit = null)
        {
            BeginDeath();
        }

        //记录受击姿势并立即结束战斗，再播放不造成伤害的出土与倒伏。
        private void BeginDeath()
        {
            if (isDying || Destroyed) return;
            deathSourceState = visualState;
            deathSourceProgress = Mathf.Clamp01(CombatVisualProgress);
            deathStartOffset = GetDeathStartOffset();
            deathEmergenceStart = deathSourceState == HiveLordVisualState.Emerging
                ? Mathf.Min(0.96f, deathSourceProgress)
                : Mathf.Lerp(0.04f, 0.65f, Mathf.Clamp01(1f + deathStartOffset / ModelSinkDepth));
            deathEmergenceTicks = Mathf.Max(30,
                HiveLordCombatTiming.GetDurationTicks(HiveLordVisualState.Emerging, 1f - deathEmergenceStart));
            deathSlamTicks = HiveLordCombatTiming.GetDurationTicks(
                HiveLordVisualState.GroundSlam, CombatSettings.slamImpactNormalizedTime);
            isDying = true;
            deathElapsedTicks = 0;
            deathImpactPlayed = false;
            CombatController.SetEnabled(false);
            automaticCycle = false;
            RetireHitProxy();
            storedHitPoints = 0;
            visualState = HiveLordVisualState.Emerging;
            restartRequested = true;
            stateRevision++;
            if (Spawned)
            {
                Map.GetComponent<HiveLordChallengeMapComponent>().NotifyBossDying(this);
                HiveLordCombatEffects.SpawnEmergenceBurst(this);
                HiveLordSoundPlayer.PlayEmergence(this);
            }
        }

        //按受击前的出入土进度还原模型埋深，保持死亡首帧高度连续。
        private float GetDeathStartOffset()
        {
            switch (deathSourceState)
            {
                case HiveLordVisualState.Underground: return -ModelSinkDepth;
                case HiveLordVisualState.Emerging:
                    return Mathf.Lerp(-ModelSinkDepth, 0f, Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.03f, 0.96f, deathSourceProgress)));
                case HiveLordVisualState.Submerging:
                    return Mathf.Lerp(0f, -ModelSinkDepth, Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.08f, 0.97f, deathSourceProgress)));
                default: return 0f;
            }
        }

        //推进临终动画，并在实际触地时只播放一次冲击，随后定格下沉。
        private void TickDeath()
        {
            deathElapsedTicks++;
            if (DeathEmerging) HiveLordCombatEffects.TickEmergenceDebris(this);
            if (deathElapsedTicks == deathEmergenceTicks)
            {
                visualState = HiveLordVisualState.GroundSlam;
                stateRevision++;
            }
            if (!deathImpactPlayed && deathElapsedTicks >= DeathImpactTick)
            {
                deathImpactPlayed = true;
                HiveLordCombatEffects.SpawnDeathImpact(this);
                HiveLordSoundPlayer.PlayGroundSlamImpact(this);
            }
            if (deathElapsedTicks >= DeathImpactTick + DeathHoldTicks + DeathSinkTicks)
            {
                Destroy(DestroyMode.Vanish);
                return;
            }
            if (DeathSinkProgress > 0f && deathElapsedTicks % 12 == 0)
                HiveLordCombatEffects.TickDeathDust(this);
        }

        //保存临终姿势、阶段时长和冲击标记，读档及离屏后按同一时间线恢复。
        private void ExposeDeathData()
        {
            Scribe_Values.Look(ref isDying, "isDying", false);
            Scribe_Values.Look(ref deathElapsedTicks, "deathElapsedTicks", 0);
            Scribe_Values.Look(ref deathStartOffset, "deathStartOffset", 0f);
            Scribe_Values.Look(ref deathSourceState, "deathSourceState", HiveLordVisualState.Underground);
            Scribe_Values.Look(ref deathSourceProgress, "deathSourceProgress", 0f);
            Scribe_Values.Look(ref deathEmergenceStart, "deathEmergenceStart", 0f);
            Scribe_Values.Look(ref deathEmergenceTicks, "deathEmergenceTicks", 0);
            Scribe_Values.Look(ref deathSlamTicks, "deathSlamTicks", 0);
            Scribe_Values.Look(ref deathImpactPlayed, "deathImpactPlayed", false);
        }
    }
}

