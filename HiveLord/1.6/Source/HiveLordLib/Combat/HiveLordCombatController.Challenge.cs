using System;
using Verse;

namespace HiveLordLib
{
    //提供任务定时登场入口，复用正常出土动作与攻击选择并跳过首轮地下等待。
    internal sealed partial class HiveLordCombatController
    {
        //锁定挑战队伍，按现有攻击半径选择出土位置并立即开始现身。
        internal void BeginChallengeEncounter(Pawn target)
        {
            if (target == null || !HiveLordChallengeParticipants.IsActive(target) || target.MapHeld != owner.Map)
                throw new InvalidOperationException("HiveLord_Error_ChallengeTarget".Translate().ToString());
            enabled = true;
            owner.SetAutomaticCycleFromCombat(false);
            ResetToUnderground(true, false);
            //未离开运输容器的乘员只提供登场位置，不作为普通地表攻击目标保存。
            combatTarget = target.Spawned ? target : null;
            forcedTarget = combatTarget;
            ChooseNextAttack();
            if (!HiveLordEmergencePlanner.TryChooseCell(owner.Map, target.PositionHeld, plannedAttack, owner.CombatSettings, out IntVec3 cell))
                throw new InvalidOperationException("HiveLord_Error_EmergenceCell".Translate().ToString());
            aimCell = target.PositionHeld;
            owner.Position = cell;
            GetDamageAreaCells();
            HiveLordCombatEffects.SpawnEmergenceWarning(owner);
            BeginEmerging();
        }
    }
}
