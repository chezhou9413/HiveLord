using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //同步 Thing 视觉状态、Animator 动画、吐酸粒子和出土限时粒子。
    internal sealed partial class HiveLordVisualController
    {
        private const float EmergenceEffectDuration = 3f;
        private const float ModelSinkDepth = HiveLordProjectionThing.ModelSinkDepth;
        private const float AcidEmissionMultiplier = 2.6f;
        private const float AcidLifetimeMultiplier = 1.35f;
        private const float AcidSizeMultiplier = 1.2f;

        private static readonly HiveLordVisualState[] AutomaticSequence =
        {
            HiveLordVisualState.Underground,
            HiveLordVisualState.Emerging,
            HiveLordVisualState.Idle,
            HiveLordVisualState.AcidAttack,
            HiveLordVisualState.Idle,
            HiveLordVisualState.GroundSlam,
            HiveLordVisualState.Submerging
        };

        private readonly Animator animator;
        private readonly ParticleSystem[] allParticles;
        private readonly ParticleSystem[] acidParticles;
        private readonly ParticleSystem emergenceParticle;
        private readonly SkinnedMeshRenderer[] bodyRenderers;
        private readonly HiveLordAnimationCatalog animationCatalog;
        private int observedRevision = int.MinValue;
        private int sequenceIndex;
        private float stateElapsed;
        private float emergenceEffectElapsed;
        private bool advancingAutomatically;
        private bool acidEmissionActive;
        private bool emergenceEffectPlaying;
        private bool resyncRequested = true;
        private int lastVisualTick;

        //按稳定名称验证两层酸液粒子与出土粒子，并清除预制体中可能残留的发射状态。
        public HiveLordVisualController(Animator configuredAnimator, ParticleSystem[] configuredParticles)
        {
            animator = configuredAnimator ?? throw new ArgumentNullException(nameof(configuredAnimator));
            allParticles = configuredParticles ?? throw new ArgumentNullException(nameof(configuredParticles));
            acidParticles = allParticles
                .Where(system => system != null
                    && (system.name == "HiveLord_AcidCore" || system.name == "HiveLord_AcidMist"))
                .ToArray();
            emergenceParticle = allParticles.SingleOrDefault(
                system => system != null && system.name == "HiveLord_EmergenceBurst");
            if (allParticles.Length != 3 || acidParticles.Length != 2 || emergenceParticle == null)
            {
                throw new InvalidOperationException("HiveLord_Error_ParticleLayout".Translate().ToString());
            }

            animationCatalog = new HiveLordAnimationCatalog(animator);
            animator.speed = 0f;
            bodyRenderers = animator.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (bodyRenderers.Length == 0)
            {
                throw new InvalidOperationException("HiveLord_Error_SkinnedMesh".Translate().ToString());
            }

            for (int index = 0; index < allParticles.Length; index++)
            {
                allParticles[index].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            ConfigureAcidDensity();
        }

        //按游戏 Tick 同步姿势、粒子窗口与自动展示，在捕获前完成当前动画求值。
        public void Update(HiveLordProjectionThing thing, bool paused)
        {
            if (thing.IsDying)
            {
                UpdateDeathPose(thing);
                return;
            }
            SyncState(thing);
            //动画与出入土高度共享同一份游戏进度，禁止 Unity 在另一时钟上自主推进骨骼。
            animator.speed = 0f;
            int currentTick = Find.TickManager.TicksGame;
            float elapsed = Mathf.Max(0, currentTick - lastVisualTick) / 60f;
            lastVisualTick = currentTick;
            if (thing.CombatAiEnabled)
            {
                stateElapsed = animationCatalog.GetDuration(thing.VisualState) * thing.CombatVisualProgress;
            }
            else if (!paused)
            {
                stateElapsed += elapsed;
            }
            SyncParticlePause(paused);
            if (!paused)
            {
                UpdateAcidEmission(thing, stateElapsed);
                UpdateEmergenceEffect(thing.VisualState, elapsed);
            }

            if (!paused && thing.AutomaticCycle
                && stateElapsed >= animationCatalog.GetDuration(thing.VisualState))
            {
                sequenceIndex = (sequenceIndex + 1) % AutomaticSequence.Length;
                advancingAutomatically = true;
                thing.SetVisualState(AutomaticSequence[sequenceIndex], true);
                SyncState(thing);
            }

            animationCatalog.PlayAt(animator, thing.VisualState,
                stateElapsed / animationCatalog.GetDuration(thing.VisualState));
            SetBodyVisible(thing.VisualState != HiveLordVisualState.Underground);
        }

        //要求下一次可见更新按 Thing 保存的阶段进度精确重定位动画。
        public void RequestResync()
        {
            resyncRequested = true;
        }

        //按出入土动画进度返回虫体根节点的纵向位移，使模型完整穿过地面裁剪线。
        public float GetModelVerticalOffset(HiveLordProjectionThing thing)
        {
            if (thing.IsDying) return thing.DeathVerticalOffset;
            float progress = thing.CombatAiEnabled
                ? thing.CombatVisualProgress
                : stateElapsed / animationCatalog.GetDuration(thing.VisualState);
            progress = Mathf.Clamp01(progress);
            switch (thing.VisualState)
            {
                case HiveLordVisualState.Underground:
                    return -ModelSinkDepth;
                case HiveLordVisualState.Emerging:
                    float emergenceBlend = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(0.03f, 0.96f, progress));
                    return Mathf.Lerp(-ModelSinkDepth, 0f, emergenceBlend);
                case HiveLordVisualState.Submerging:
                    float submergingBlend = Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.InverseLerp(0.08f, 0.97f, progress));
                    return Mathf.Lerp(0f, -ModelSinkDepth, submergingBlend);
                default:
                    return 0f;
            }
        }

        //同步阶段进度与粒子状态，骨骼在统一更新末尾求值后才允许参与捕获。
        private void SyncState(HiveLordProjectionThing thing)
        {
            if (observedRevision == thing.StateRevision && !resyncRequested)
            {
                return;
            }

            bool stateChanged = observedRevision != thing.StateRevision;
            observedRevision = thing.StateRevision;
            if (thing.CombatAiEnabled)
            {
                stateElapsed = animationCatalog.GetDuration(thing.VisualState) * thing.CombatVisualProgress;
            }
            else if (stateChanged)
            {
                stateElapsed = 0f;
            }

            float normalizedProgress = stateElapsed / animationCatalog.GetDuration(thing.VisualState);
            lastVisualTick = Find.TickManager.TicksGame;
            resyncRequested = false;
            ResetAcidEmission(thing, normalizedProgress);
            SetEmergenceEmission(thing.VisualState == HiveLordVisualState.Emerging, stateElapsed);

            if (!advancingAutomatically)
            {
                sequenceIndex = FindSequenceIndex(thing.VisualState);
            }

            advancingAutomatically = false;
        }

        //地下阶段关闭全部虫体蒙皮渲染器，其余阶段恢复显示。
        private void SetBodyVisible(bool shouldBeVisible)
        {
            for (int index = 0; index < bodyRenderers.Length; index++)
            {
                bodyRenderers[index].enabled = shouldBeVisible;
            }
        }

        //把外部指定状态对齐到自动展示序列中的首个相同阶段。
        private static int FindSequenceIndex(HiveLordVisualState state)
        {
            for (int index = 0; index < AutomaticSequence.Length; index++)
            {
                if (AutomaticSequence[index] == state)
                {
                    return index;
                }
            }

            return 0;
        }
    }
}
