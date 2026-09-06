using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //按战斗动画进度生成带距离衰减的持续镜头震动，并让震动峰值对齐实际动作帧。
    internal static class HiveLordScreenShake
    {
        //为地下预警、出土、吐酸、砸地和钻地阶段计算本 Tick 应维持的镜头震幅。
        public static void Tick(
            HiveLordProjectionThing owner,
            HiveLordCombatPhase phase,
            float clipProgress,
            float timedPhaseProgress,
            HiveLordCombatExtension settings)
        {
            if (owner.Map != Find.CurrentMap || Find.CameraDriver == null)
            {
                return;
            }

            float magnitude = GetMagnitude(phase, clipProgress, timedPhaseProgress, settings);
            if (magnitude <= 0f)
            {
                return;
            }

            float cameraDistance = (owner.Position - Find.CameraDriver.MapPosition).LengthHorizontal;
            float distanceFactor = 1f - Mathf.Clamp01(cameraDistance / settings.screenShakeDistance);
            float attenuatedMagnitude = magnitude * Mathf.Lerp(0.25f, 1f, distanceFactor);
            Find.CameraDriver.shaker.SetMinShake(attenuatedMagnitude);
        }

        //根据阶段返回与动画动作同步的震幅，吐酸发射和砸地触地之外不产生攻击震动。
        private static float GetMagnitude(
            HiveLordCombatPhase phase,
            float clipProgress,
            float timedPhaseProgress,
            HiveLordCombatExtension settings)
        {
            switch (phase)
            {
                case HiveLordCombatPhase.Underground:
                    return timedPhaseProgress <= 0f
                        ? 0f
                        : settings.emergenceShakeMagnitude
                            * Mathf.Lerp(0.22f, 0.65f, Mathf.Clamp01(timedPhaseProgress));
                case HiveLordCombatPhase.Emerging:
                    return settings.emergenceShakeMagnitude
                        * Mathf.Lerp(
                            0.65f,
                            1f,
                            Mathf.Clamp01(clipProgress / settings.emergenceExitNormalizedTime));
                case HiveLordCombatPhase.AcidAttack:
                    return GetAcidMagnitude(clipProgress, settings);
                case HiveLordCombatPhase.GroundSlam:
                    return GetSlamMagnitude(clipProgress, settings);
                case HiveLordCombatPhase.Submerging:
                    return settings.submergingShakeMagnitude
                        * Mathf.Lerp(
                            1f,
                            0.45f,
                            Mathf.Clamp01(clipProgress / settings.submergingExitNormalizedTime));
                default:
                    return 0f;
            }
        }

        //只在酸液粒子实际发射窗口维持震动，并在窗口中央形成轻微强度起伏。
        private static float GetAcidMagnitude(float clipProgress, HiveLordCombatExtension settings)
        {
            if (clipProgress < settings.acidEmissionStartNormalizedTime
                || clipProgress > settings.acidEmissionEndNormalizedTime)
            {
                return 0f;
            }

            float sprayProgress = Mathf.InverseLerp(
                settings.acidEmissionStartNormalizedTime,
                settings.acidEmissionEndNormalizedTime,
                clipProgress);
            return settings.acidShakeMagnitude
                * Mathf.Lerp(0.82f, 1f, Mathf.Sin(sprayProgress * Mathf.PI));
        }

        //让砸地震动从蓄力阶段逐渐增强，在实际触地伤害帧达到峰值后持续衰减。
        private static float GetSlamMagnitude(float clipProgress, HiveLordCombatExtension settings)
        {
            float start = Mathf.Max(0.05f, settings.slamImpactNormalizedTime - 0.22f);
            float end = Mathf.Min(
                settings.attackExitNormalizedTime,
                settings.slamImpactNormalizedTime + 0.25f);
            if (clipProgress < start || clipProgress > end)
            {
                return 0f;
            }

            if (clipProgress <= settings.slamImpactNormalizedTime)
            {
                float buildup = Mathf.InverseLerp(start, settings.slamImpactNormalizedTime, clipProgress);
                return settings.slamShakeMagnitude * Mathf.Lerp(0.3f, 1f, buildup);
            }

            float decay = Mathf.InverseLerp(settings.slamImpactNormalizedTime, end, clipProgress);
            return settings.slamShakeMagnitude * Mathf.Lerp(1f, 0.28f, decay);
        }
    }
}
