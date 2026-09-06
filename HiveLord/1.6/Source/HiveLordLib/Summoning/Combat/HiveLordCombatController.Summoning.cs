using Verse;

namespace HiveLordLib
{
    //从技能落点启动召唤体，不要求地图当前已有敌人。
    internal sealed partial class HiveLordCombatController
    {
        //跳过首次地下等待并播放出土，之后由原有状态机自主战斗。
        internal void BeginSummonedEncounter()
        {
            enabled = true;
            owner.SetAutomaticCycleFromCombat(false);
            ResetToUnderground(true, false);
            combatTarget = HiveLordTargetFinder.FindClosest(owner, owner.CombatSettings);
            aimCell = combatTarget?.Position ?? owner.Position;
            ChooseNextAttack();
            GetDamageAreaCells();
            HiveLordCombatEffects.SpawnEmergenceWarning(owner);
            BeginEmerging();
        }
    }
}
