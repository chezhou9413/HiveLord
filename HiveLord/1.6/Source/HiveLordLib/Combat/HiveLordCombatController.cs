using System;
using System.Collections.Generic;
using Verse;

namespace HiveLordLib
{
    //驱动霸王虫从地下锁敌、出土、单次攻击到再次钻地的可存档战斗循环。
    internal sealed partial class HiveLordCombatController
    {
        private readonly HiveLordProjectionThing owner;
        private readonly HiveLordAttackResolver attackResolver = new HiveLordAttackResolver();
        private readonly List<IntVec3> damageAreaCells = new List<IntVec3>();
        private bool enabled = true;
        private bool stateInitialized;
        private Thing combatTarget;
        private Thing forcedTarget;
        private HiveLordCombatPhase phase = HiveLordCombatPhase.Underground;
        private HiveLordPlannedAttack plannedAttack = HiveLordPlannedAttack.Acid;
        private int phaseTotalTicks;
        private int phaseRemainingTicks;
        private int searchTicksRemaining;
        private int undergroundRestTicksRemaining;
        private int groundFeedbackTicksRemaining;
        private bool attackSequenceInitialized;
        private bool emergenceShockwaveApplied;
        private bool impactApplied;
        private IntVec3 aimCell = IntVec3.Invalid;
        private IntVec3 cachedAreaOrigin = IntVec3.Invalid;
        private IntVec3 cachedAreaAim = IntVec3.Invalid;
        private HiveLordPlannedAttack cachedAreaAttack;

        public bool Enabled => enabled;
        public Thing CombatTarget => combatTarget;
        public IntVec3 AimCell => aimCell;
        public HiveLordCombatPhase Phase => phase;

        //绑定承载状态和地图位置的霸王虫 Thing。
        public HiveLordCombatController(HiveLordProjectionThing configuredOwner)
        {
            owner = configuredOwner ?? throw new ArgumentNullException(nameof(configuredOwner));
        }

        //每个正常 Tick 验证目标并推进当前战斗阶段一次。
        public void Tick()
        {
            if (!enabled || !owner.Spawned || owner.IsDying)
            {
                return;
            }

            TickGroundFeedback();

            ValidateTargets();
            if (combatTarget != null
                && phase == HiveLordCombatPhase.Underground
                && phaseTotalTicks == 0)
            {
                aimCell = combatTarget.Position;
            }

            switch (phase)
            {
                case HiveLordCombatPhase.Underground:
                    TickUnderground();
                    break;
                case HiveLordCombatPhase.Emerging:
                    TickEmerging();
                    break;
                case HiveLordCombatPhase.SurfaceIdle:
                    TickTimedPhase(BeginAttackOrSubmerge);
                    break;
                case HiveLordCombatPhase.AcidAttack:
                case HiveLordCombatPhase.GroundSlam:
                    TickAttack();
                    break;
                case HiveLordCombatPhase.Recovery:
                    TickTimedPhase(BeginSubmerging);
                    break;
                case HiveLordCombatPhase.Submerging:
                    TickTimedPhase(FinishSubmerging);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(phase), phase, "HiveLord_Error_CombatPhase".Translate().ToString());
            }

            TickScreenShake();
        }

        //把当前战斗阶段、动画进度和地下预警进度交给镜头震动曲线处理。
        private void TickScreenShake()
        {
            float timedPhaseProgress = phaseTotalTicks <= 0
                ? 0f
                : (phaseTotalTicks - phaseRemainingTicks) / (float)phaseTotalTicks;
            HiveLordScreenShake.Tick(
                owner,
                phase,
                GetVisualProgress(),
                timedPhaseProgress,
                owner.CombatSettings);
        }

        //切换 AI；重新开启时关闭展示循环并从全新的地下阶段开始。
        public void SetEnabled(bool shouldEnable)
        {
            if (shouldEnable && owner.IsDying) return;
            if (enabled == shouldEnable)
            {
                return;
            }

            enabled = shouldEnable;
            if (!enabled)
            {
                return;
            }

            owner.SetAutomaticCycleFromCombat(false);
            ResetToUnderground(true, true);
        }

        //设置下一轮优先使用的强制目标，传入空值时恢复自动锁敌。
        public void SetForcedTarget(Thing target)
        {
            if (target == null)
            {
                forcedTarget = null;
                if (phase == HiveLordCombatPhase.Underground && phaseTotalTicks == 0)
                {
                    combatTarget = null;
                    searchTicksRemaining = 0;
                }

                return;
            }

            if (!HiveLordTargetFinder.IsLockable(
                    owner,
                    target,
                    owner.CombatSettings.targetRetentionRadius,
                    false))
            {
                forcedTarget = null;
                return;
            }

            forcedTarget = target;
            if (phase == HiveLordCombatPhase.Underground && phaseTotalTicks == 0)
            {
                combatTarget = target;
                searchTicksRemaining = 0;
            }
        }

        //清除当前与强制目标，并让地下阶段在下一个 Tick 立即重新搜索。
        public void ClearTargetAndReacquire()
        {
            if (owner.IsDying) return;
            combatTarget = null;
            forcedTarget = null;
            searchTicksRemaining = 0;
            if (phase != HiveLordCombatPhase.Underground || phaseTotalTicks != 0)
            {
                ResetToUnderground(false, false);
            }
        }
    }
}
