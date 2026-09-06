using System;
using Verse;

namespace HiveLordLib
{
    //实现霸王虫各战斗阶段的计时推进、攻击衔接与目标失效处理。
    internal sealed partial class HiveLordCombatController
    {
        //分别绘制本轮锁定区域的预警和上一轮已经命中的独立地面反馈。
        public void DrawDamageArea()
        {
            bool warningActive = !impactApplied
                && (phase == HiveLordCombatPhase.SurfaceIdle
                    || phase == HiveLordCombatPhase.AcidAttack
                    || phase == HiveLordCombatPhase.GroundSlam);
            if (!enabled)
            {
                return;
            }

            if (warningActive && HiveLordMod.Settings.showAttackWarnings)
            {
                GetDamageAreaCells();
                if (plannedAttack == HiveLordPlannedAttack.Acid)
                {
                    HiveLordCombatEffects.DrawAcidDamageArea(damageAreaCells, false);
                }
                else
                {
                    HiveLordCombatEffects.DrawSlamDamageArea(damageAreaCells, false);
                }
            }

            DrawGroundFeedback();
        }

        //在地下等待阶段按搜索间隔锁敌、选招、选落点并开始移动计时。
        private void TickUnderground()
        {
            if (phaseTotalTicks > 0)
            {
                HiveLordCombatEffects.TickEmergenceWarning(owner, phaseRemainingTicks);
                TickTimedPhase(BeginEmerging);
                return;
            }

            if (undergroundRestTicksRemaining > 0)
            {
                undergroundRestTicksRemaining--;
                return;
            }

            if (searchTicksRemaining > 0)
            {
                searchTicksRemaining--;
                return;
            }

            searchTicksRemaining = Math.Max(0, owner.CombatSettings.targetSearchIntervalTicks - 1);
            combatTarget = ResolveUndergroundTarget();
            if (combatTarget == null)
            {
                return;
            }

            ChooseNextAttack();
            if (!HiveLordEmergencePlanner.TryChooseCell(
                    owner,
                    combatTarget,
                    plannedAttack,
                    owner.CombatSettings,
                    out IntVec3 emergenceCell))
            {
                return;
            }

            aimCell = combatTarget.Position;
            owner.Position = emergenceCell;
            undergroundRestTicksRemaining = 0;
            GetDamageAreaCells();
            HiveLordCombatEffects.SpawnEmergenceWarning(owner);
            HiveLordSoundPlayer.PlayUndergroundTravel(owner);
            BeginPhase(
                HiveLordCombatPhase.Underground,
                owner.CombatSettings.undergroundTravelTicks,
                HiveLordVisualState.Underground);
        }

        //解析强制目标或自动搜索结果，强制目标失效后自动回到普通搜索。
        private Thing ResolveUndergroundTarget()
        {
            if (forcedTarget != null)
            {
                return forcedTarget;
            }

            return HiveLordTargetFinder.FindClosest(owner, owner.CombatSettings);
        }

        //地下移动结束后沿最后保存的瞄准方向开始播放出土动画。
        private void BeginEmerging()
        {
            if (!aimCell.IsValid || !aimCell.InBounds(owner.Map))
            {
                ResetToUnderground(false, true);
                return;
            }

            emergenceShockwaveApplied = false;
            HiveLordSoundPlayer.PlayEmergence(owner);
            BeginPhase(
                HiveLordCombatPhase.Emerging,
                HiveLordCombatTiming.GetDurationTicks(
                    HiveLordVisualState.Emerging,
                    owner.CombatSettings.emergenceExitNormalizedTime),
                HiveLordVisualState.Emerging);
        }

        //在虫体真正破土的动画关键帧只触发一次大范围冲击、碎石和 Pawn 击飞，再衔接地表待机。
        private void TickEmerging()
        {
            AdvanceTimer();
            int elapsedTicks = phaseTotalTicks - phaseRemainingTicks;
            int shockwaveTick = Math.Max(
                1,
                (int)Math.Ceiling(
                    phaseTotalTicks * owner.CombatSettings.emergenceShockwaveNormalizedTime));
            if (!emergenceShockwaveApplied && elapsedTicks >= shockwaveTick)
            {
                emergenceShockwaveApplied = true;
                HiveLordBurrowPushUtility.PushNearbyPawns(owner, owner.CombatSettings);
                HiveLordCombatEffects.SpawnEmergenceShockwave(owner, owner.CombatSettings);
                HiveLordSoundPlayer.PlayEmergenceImpact(owner);
            }

            if (phaseRemainingTicks <= 0)
            {
                BeginSurfaceIdle();
            }
        }

        //出土动画结束后进入固定 150 Tick 的地表待机阶段。
        private void BeginSurfaceIdle()
        {
            HiveLordSoundPlayer.PlayAttackWarning(owner, plannedAttack);
            BeginPhase(
                HiveLordCombatPhase.SurfaceIdle,
                owner.CombatSettings.surfaceIdleTicks,
                HiveLordVisualState.Idle);
        }

        //待机结束后无条件衔接本轮计划的攻击，目标失效也不会中断已经预警的招式。
        private void BeginAttackOrSubmerge()
        {
            HiveLordVisualState visualState = plannedAttack == HiveLordPlannedAttack.Acid
                ? HiveLordVisualState.AcidAttack
                : HiveLordVisualState.GroundSlam;
            HiveLordCombatPhase attackPhase = plannedAttack == HiveLordPlannedAttack.Acid
                ? HiveLordCombatPhase.AcidAttack
                : HiveLordCombatPhase.GroundSlam;
            BeginPhase(
                attackPhase,
                HiveLordCombatTiming.GetDurationTicks(
                    visualState,
                    owner.CombatSettings.attackExitNormalizedTime),
                visualState);
        }

        //分别在吐酸喷射命中帧和砸地触地帧只结算一次伤害，并在攻击结束后进入恢复阶段。
        private void TickAttack()
        {
            AdvanceTimer();
            int elapsedTicks = phaseTotalTicks - phaseRemainingTicks;
            HiveLordVisualState attackState = phase == HiveLordCombatPhase.AcidAttack
                ? HiveLordVisualState.AcidAttack
                : HiveLordVisualState.GroundSlam;
            int impactTick = HiveLordCombatTiming.GetDurationTicks(
                attackState,
                phase == HiveLordCombatPhase.AcidAttack
                    ? owner.CombatSettings.acidImpactNormalizedTime
                    : owner.CombatSettings.slamImpactNormalizedTime);
            if (!impactApplied && elapsedTicks >= impactTick)
            {
                impactApplied = true;
                ResolveImpact();
            }

            if (phaseRemainingTicks <= 0)
            {
                BeginPhase(
                    HiveLordCombatPhase.Recovery,
                    owner.CombatSettings.attackRecoveryTicks,
                    HiveLordVisualState.Idle);
            }
        }

        //在命中帧对已经预警并锁定的瞄准格结算范围伤害，使画面、警示圈和实际受击区一致。
        private void ResolveImpact()
        {
            if (!aimCell.IsValid || !aimCell.InBounds(owner.Map))
            {
                return;
            }

            bool impactCreated;
            GetDamageAreaCells();
            if (phase == HiveLordCombatPhase.AcidAttack)
            {
                impactCreated = attackResolver.ResolveAcid(
                    owner,
                    damageAreaCells,
                    owner.CombatSettings);
            }
            else
            {
                impactCreated = attackResolver.ResolveGroundSlam(
                    owner,
                    damageAreaCells,
                    owner.CombatSettings);
            }

            if (impactCreated)
            {
                CaptureGroundFeedback();
                if (phase == HiveLordCombatPhase.GroundSlam)
                {
                    HiveLordSoundPlayer.PlayGroundSlamImpact(owner);
                }
            }
        }

        //首轮按配置随机选招，之后严格在吐酸与砸地之间交替，避免连续重复同一招。
        private void ChooseNextAttack()
        {
            if (!attackSequenceInitialized)
            {
                plannedAttack = Rand.Chance(owner.CombatSettings.acidAttackChance)
                    ? HiveLordPlannedAttack.Acid
                    : HiveLordPlannedAttack.GroundSlam;
                attackSequenceInitialized = true;
                return;
            }

            plannedAttack = plannedAttack == HiveLordPlannedAttack.Acid
                ? HiveLordPlannedAttack.GroundSlam
                : HiveLordPlannedAttack.Acid;
        }

        //恢复阶段结束后开始播放钻入地下动画。
        private void BeginSubmerging()
        {
            HiveLordSoundPlayer.PlaySubmerging(owner);
            BeginPhase(
                HiveLordCombatPhase.Submerging,
                HiveLordCombatTiming.GetDurationTicks(
                    HiveLordVisualState.Submerging,
                    owner.CombatSettings.submergingExitNormalizedTime)
                    + owner.CombatSettings.submergingHideDelayTicks,
                HiveLordVisualState.Submerging);
        }

        //钻地结束后保留最后瞄准格，清除当前目标并立即准备下一轮搜索。
        private void FinishSubmerging()
        {
            combatTarget = null;
            ResetToUnderground(false, true);
        }

        //推进普通计时阶段，并在倒计时归零时调用阶段衔接函数。
        private void TickTimedPhase(Action onCompleted)
        {
            AdvanceTimer();
            if (phaseRemainingTicks <= 0)
            {
                onCompleted();
            }
        }

        //把当前阶段剩余 Tick 减少一次且不产生负数。
        private void AdvanceTimer()
        {
            if (phaseRemainingTicks > 0)
            {
                phaseRemainingTicks--;
            }
        }

        //初始化一个有时长的阶段并通知渲染端从该动画起点播放。
        private void BeginPhase(
            HiveLordCombatPhase nextPhase,
            int durationTicks,
            HiveLordVisualState visualState)
        {
            phase = nextPhase;
            bool interval = nextPhase == HiveLordCombatPhase.Underground
                || nextPhase == HiveLordCombatPhase.SurfaceIdle
                || nextPhase == HiveLordCombatPhase.Recovery;
            phaseTotalTicks = Math.Max(1, interval ? HiveLordDifficulty.ScaleInterval(durationTicks) : durationTicks);
            phaseRemainingTicks = phaseTotalTicks;
            impactApplied = false;
            owner.SetVisualStateFromCombat(visualState, true);
        }

        //进入地下状态，并按配置决定是否安排下一轮随机潜伏等待。
        private void ResetToUnderground(bool clearAim, bool scheduleRest)
        {
            phase = HiveLordCombatPhase.Underground;
            phaseTotalTicks = 0;
            phaseRemainingTicks = 0;
            searchTicksRemaining = 0;
            impactApplied = false;
            combatTarget = null;
            undergroundRestTicksRemaining = scheduleRest
                ? HiveLordDifficulty.ScaleInterval(Rand.RangeInclusive(
                    owner.CombatSettings.undergroundRestMinTicks,
                    owner.CombatSettings.undergroundRestMaxTicks))
                : 0;
            if (scheduleRest && owner.Spawned)
            {
                HiveLordSoundPlayer.PlayUndergroundPresence(owner);
            }

            if (clearAim)
            {
                aimCell = IntVec3.Invalid;
            }

            owner.SetVisualStateFromCombat(HiveLordVisualState.Underground, true);
        }

        //每 Tick 清除死亡、倒地、离图、失去敌对关系或超出全图保留范围的引用。
        private void ValidateTargets()
        {
            float retentionRadius = owner.CombatSettings.targetRetentionRadius;
            if (forcedTarget != null
                && !HiveLordTargetFinder.IsLockable(owner, forcedTarget, retentionRadius, false))
            {
                forcedTarget = null;
            }

            if (combatTarget != null
                && !HiveLordTargetFinder.IsLockable(owner, combatTarget, retentionRadius, false))
            {
                combatTarget = null;
            }
        }
    }
}
