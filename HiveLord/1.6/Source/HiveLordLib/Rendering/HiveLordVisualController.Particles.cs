using UnityEngine;

namespace HiveLordLib
{
    //控制酸液核心与薄雾分层、出土粒子及游戏暂停同步。
    internal sealed partial class HiveLordVisualController
    {
        //状态切换或离屏恢复时清空旧酸液，并按当前动画进度恢复正确发射阶段。
        private void ResetAcidEmission(HiveLordProjectionThing thing, float normalizedProgress)
        {
            for (int index = 0; index < acidParticles.Length; index++)
            {
                acidParticles[index].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            acidEmissionActive = false;
            UpdateAcidEmission(
                thing,
                animationCatalog.GetDuration(thing.VisualState) * normalizedProgress);
        }

        //只在吐酸动画喷射动作区间启用两层酸液，使粒子和实际命中帧衔接。
        private void UpdateAcidEmission(HiveLordProjectionThing thing, float elapsed)
        {
            float duration = animationCatalog.GetDuration(HiveLordVisualState.AcidAttack);
            float normalizedProgress = elapsed / duration;
            bool shouldEmit = thing.VisualState == HiveLordVisualState.AcidAttack
                && normalizedProgress >= thing.CombatSettings.acidEmissionStartNormalizedTime
                && normalizedProgress < thing.CombatSettings.acidEmissionEndNormalizedTime;
            if (acidEmissionActive == shouldEmit)
            {
                return;
            }

            acidEmissionActive = shouldEmit;
            for (int index = 0; index < acidParticles.Length; index++)
            {
                ParticleSystem particleSystem = acidParticles[index];
                if (shouldEmit)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    particleSystem.Play(true);
                    continue;
                }

                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        //出土阶段开始时恢复发射，离开阶段时仅停发并保留已经生成的烟雾自然消散。
        private void SetEmergenceEmission(bool shouldEmit, float elapsed)
        {
            emergenceEffectElapsed = Mathf.Max(0f, elapsed);
            emergenceEffectPlaying = shouldEmit && emergenceEffectElapsed < EmergenceEffectDuration;
            if (emergenceEffectPlaying)
            {
                emergenceParticle.Play(true);
                return;
            }

            emergenceParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        //按 RimWorld 游戏时间把出土粒子的发射时长限制为三秒，已有粒子继续完成生命周期。
        private void UpdateEmergenceEffect(HiveLordVisualState state, float elapsed)
        {
            if (!emergenceEffectPlaying)
            {
                return;
            }

            if (state != HiveLordVisualState.Emerging)
            {
                emergenceParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                emergenceEffectPlaying = false;
                return;
            }

            emergenceEffectElapsed += elapsed;
            if (emergenceEffectElapsed < EmergenceEffectDuration)
            {
                return;
            }

            emergenceParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            emergenceEffectPlaying = false;
        }

        //随 RimWorld 暂停状态冻结或恢复当前仍在播放的全部粒子。
        private void SyncParticlePause(bool paused)
        {
            for (int index = 0; index < allParticles.Length; index++)
            {
                ParticleSystem particleSystem = allParticles[index];
                if (paused)
                {
                    if (particleSystem.isPlaying)
                    {
                        particleSystem.Pause(true);
                    }

                    continue;
                }

                if (particleSystem.isPaused)
                {
                    particleSystem.Play(true);
                }
            }
        }

        //只在运行时提高现有两层酸液的发射量、停留时间和体积，不改动预制体或 AssetBundle。
        private void ConfigureAcidDensity()
        {
            for (int index = 0; index < acidParticles.Length; index++)
            {
                ParticleSystem particleSystem = acidParticles[index];
                ParticleSystem.EmissionModule emission = particleSystem.emission;
                bool core = particleSystem.name == "HiveLord_AcidCore";
                emission.rateOverTimeMultiplier *= core ? AcidEmissionMultiplier * 1.2f : AcidEmissionMultiplier * 0.6f;
                emission.rateOverDistanceMultiplier *= AcidEmissionMultiplier;

                ParticleSystem.MainModule main = particleSystem.main;
                main.startLifetimeMultiplier *= core ? AcidLifetimeMultiplier : 0.95f;
                main.startSizeMultiplier *= core ? AcidSizeMultiplier * 1.1f : AcidSizeMultiplier * 1.3f;
                main.maxParticles = Mathf.Max(main.maxParticles, 3000);
            }
        }

    }
}
