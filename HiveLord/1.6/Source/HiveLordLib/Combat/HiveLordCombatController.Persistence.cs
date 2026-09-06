using System;
using Verse;

namespace HiveLordLib
{
    //集中处理霸王虫战斗状态的存档、生成恢复和渲染进度换算。
    internal sealed partial class HiveLordCombatController
    {
        //读写全部影响战斗连续性的字段，Unity 运行时对象不进入存档。
        public void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "combatAiEnabled", true);
            Scribe_Values.Look(ref stateInitialized, "combatStateInitialized", false);
            Scribe_References.Look(ref combatTarget, "combatTarget");
            Scribe_References.Look(ref forcedTarget, "forcedCombatTarget");
            Scribe_Values.Look(ref phase, "combatPhase", HiveLordCombatPhase.Underground);
            Scribe_Values.Look(ref plannedAttack, "plannedAttack", HiveLordPlannedAttack.Acid);
            Scribe_Values.Look(ref phaseTotalTicks, "phaseTotalTicks", 0);
            Scribe_Values.Look(ref phaseRemainingTicks, "phaseRemainingTicks", 0);
            Scribe_Values.Look(ref searchTicksRemaining, "targetSearchTicksRemaining", 0);
            Scribe_Values.Look(ref undergroundRestTicksRemaining, "undergroundRestTicksRemaining", 0);
            Scribe_Values.Look(ref attackSequenceInitialized, "attackSequenceInitialized", false);
            Scribe_Values.Look(ref emergenceShockwaveApplied, "emergenceShockwaveApplied", false);
            Scribe_Values.Look(ref impactApplied, "attackImpactApplied", false);
            Scribe_Values.Look(ref aimCell, "combatAimCell", IntVec3.Invalid);
            ExposeGroundFeedback();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ValidateSavedState();
            }
        }

        //生成或读档后建立首个地下阶段，并把视觉对齐到保存进度。
        public void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!stateInitialized)
            {
                stateInitialized = true;
                ResetToUnderground(true, true);
            }

            if (!enabled)
            {
                return;
            }

            owner.SetAutomaticCycleFromCombat(false);
            owner.SetVisualStateFromCombat(GetVisualState(phase), true);
        }

        //返回渲染端用于离屏恢复和读档定位动画的归一化进度。
        public float GetVisualProgress()
        {
            int elapsedTicks = Math.Max(0, phaseTotalTicks - phaseRemainingTicks);
            switch (phase)
            {
                case HiveLordCombatPhase.Emerging:
                    return GetClipProgress(HiveLordVisualState.Emerging, elapsedTicks);
                case HiveLordCombatPhase.AcidAttack:
                    return GetClipProgress(HiveLordVisualState.AcidAttack, elapsedTicks);
                case HiveLordCombatPhase.GroundSlam:
                    return GetClipProgress(HiveLordVisualState.GroundSlam, elapsedTicks);
                case HiveLordCombatPhase.Submerging:
                    return GetClipProgress(HiveLordVisualState.Submerging, elapsedTicks);
                case HiveLordCombatPhase.SurfaceIdle:
                case HiveLordCombatPhase.Recovery:
                    int idleTicks = HiveLordCombatTiming.GetDurationTicks(HiveLordVisualState.Idle);
                    return (elapsedTicks % idleTicks) / (float)idleTicks;
                default:
                    return 0f;
            }
        }

        //把保存的阶段映射到 Animator 使用的六段视觉状态。
        private static HiveLordVisualState GetVisualState(HiveLordCombatPhase combatPhase)
        {
            switch (combatPhase)
            {
                case HiveLordCombatPhase.Underground:
                    return HiveLordVisualState.Underground;
                case HiveLordCombatPhase.Emerging:
                    return HiveLordVisualState.Emerging;
                case HiveLordCombatPhase.SurfaceIdle:
                case HiveLordCombatPhase.Recovery:
                    return HiveLordVisualState.Idle;
                case HiveLordCombatPhase.AcidAttack:
                    return HiveLordVisualState.AcidAttack;
                case HiveLordCombatPhase.GroundSlam:
                    return HiveLordVisualState.GroundSlam;
                case HiveLordCombatPhase.Submerging:
                    return HiveLordVisualState.Submerging;
                default:
                    throw new ArgumentOutOfRangeException(nameof(combatPhase), combatPhase, null);
            }
        }

        //把当前阶段已用 Tick 转成实际 AnimationClip 的归一化进度。
        private static float GetClipProgress(HiveLordVisualState state, int elapsedTicks)
        {
            return elapsedTicks / (float)HiveLordCombatTiming.GetDurationTicks(state);
        }

        //读档后拒绝损坏或无法安全恢复的阶段枚举与倒计时数据。
        private void ValidateSavedState()
        {
            if (!Enum.IsDefined(typeof(HiveLordCombatPhase), phase)
                || !Enum.IsDefined(typeof(HiveLordPlannedAttack), plannedAttack)
                || phaseTotalTicks < 0
                || phaseRemainingTicks < 0
                || phaseRemainingTicks > phaseTotalTicks
                || undergroundRestTicksRemaining < 0
                || groundFeedbackTicksRemaining < 0)
            {
                throw new InvalidOperationException("HiveLord_Error_CombatSave".Translate().ToString());
            }
        }
    }
}
