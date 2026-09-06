using Verse;

namespace HiveLordLib
{
    //为霸王虫战斗控制器缓存预警、地面反馈和伤害共用的方向攻击区域。
    internal sealed partial class HiveLordCombatController
    {
        //按位置、瞄准格和招式缓存当前方向区域，避免每个绘制帧重复枚举地图格。
        private void GetDamageAreaCells()
        {
            if (cachedAreaOrigin == owner.Position
                && cachedAreaAim == aimCell
                && cachedAreaAttack == plannedAttack
                && damageAreaCells.Count > 0)
            {
                return;
            }

            cachedAreaOrigin = owner.Position;
            cachedAreaAim = aimCell;
            cachedAreaAttack = plannedAttack;
            if (plannedAttack == HiveLordPlannedAttack.Acid)
            {
                HiveLordAttackArea.FillAcidArea(
                    owner,
                    aimCell,
                    owner.CombatSettings,
                    damageAreaCells);
                return;
            }

            HiveLordAttackArea.FillGroundSlamArea(
                owner,
                aimCell,
                owner.CombatSettings,
                damageAreaCells);
        }
    }
}
