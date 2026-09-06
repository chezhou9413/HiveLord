using UnityEngine;

namespace HiveLordLib
{
    //按已保存死亡时间线采样出土与倒伏姿势，衔接后固定触地帧。
    internal sealed partial class HiveLordVisualController
    {
        private HiveLordPoseBlender deathPoseBlender;

        //逐帧求值临终动作，在出土和倒伏起点混合姿势，同时清除攻击粒子。
        private void UpdateDeathPose(HiveLordProjectionThing thing)
        {
            animator.speed = 0f;
            if (observedRevision != thing.StateRevision || resyncRequested)
            {
                for (int index = 0; index < allParticles.Length; index++)
                    allParticles[index].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                acidEmissionActive = false;
                emergenceEffectPlaying = false;
                observedRevision = thing.StateRevision;
                resyncRequested = false;
            }
            if (deathPoseBlender == null) deathPoseBlender = new HiveLordPoseBlender(animator);
            if (thing.DeathEmerging)
            {
                deathPoseBlender.Play(animator, animationCatalog,
                    thing.DeathSourceState, thing.DeathSourceProgress,
                    HiveLordVisualState.Emerging, thing.DeathEmergenceProgress,
                    thing.DeathElapsedTicks / (float)HiveLordProjectionThing.DeathBlendTicks);
            }
            else
            {
                deathPoseBlender.Play(animator, animationCatalog,
                    HiveLordVisualState.Emerging, 1f,
                    HiveLordVisualState.GroundSlam, thing.DeathSlamProgress,
                    (thing.DeathElapsedTicks - thing.DeathEmergenceTicks) / (float)HiveLordProjectionThing.DeathBlendTicks);
            }
            SetBodyVisible(true);
        }
    }
}
