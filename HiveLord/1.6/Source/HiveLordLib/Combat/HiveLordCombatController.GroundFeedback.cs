using System.Collections.Generic;
using Verse;

namespace HiveLordLib
{
    //保存和推进已经命中的独立地面区域，使酸液残留不会随下一轮瞄准位置移动。
    internal sealed partial class HiveLordCombatController
    {
        private List<IntVec3> feedbackAreaCells = new List<IntVec3>();
        private HiveLordPlannedAttack feedbackAttack = HiveLordPlannedAttack.Acid;

        //推进地面反馈计时，并在吐酸残留期间定期补充贴地酸雾。
        private void TickGroundFeedback()
        {
            if (groundFeedbackTicksRemaining <= 0)
            {
                return;
            }

            groundFeedbackTicksRemaining--;
            if (feedbackAttack == HiveLordPlannedAttack.Acid
                && groundFeedbackTicksRemaining > 0
                && groundFeedbackTicksRemaining % 30 == 0)
            {
                HiveLordCombatEffects.TickAcidResidue(owner.Map, feedbackAreaCells);
            }

            if (groundFeedbackTicksRemaining == 0)
            {
                feedbackAreaCells.Clear();
            }
        }

        //在命中帧复制实际区域并按招式设置互不混淆的残留时长。
        private void CaptureGroundFeedback()
        {
            feedbackAreaCells.Clear();
            feedbackAreaCells.AddRange(damageAreaCells);
            feedbackAttack = plannedAttack;
            groundFeedbackTicksRemaining = feedbackAttack == HiveLordPlannedAttack.Acid
                ? owner.CombatSettings.acidGroundDurationTicks
                : owner.CombatSettings.groundFeedbackTicks;
        }

        //绘制上一轮命中区域的条纹反馈，且不复用下一轮正在变化的瞄准数据。
        private void DrawGroundFeedback()
        {
            if (groundFeedbackTicksRemaining <= 0 || feedbackAreaCells.Count == 0)
            {
                return;
            }

            if (feedbackAttack == HiveLordPlannedAttack.Acid)
            {
                HiveLordCombatEffects.DrawAcidDamageArea(feedbackAreaCells, true);
                return;
            }

            HiveLordCombatEffects.DrawSlamDamageArea(feedbackAreaCells, true);
        }

        //保存地面残留的招式、剩余时间和锁定格，读档后继续原位置的视觉反馈。
        private void ExposeGroundFeedback()
        {
            Scribe_Values.Look(ref groundFeedbackTicksRemaining, "groundFeedbackTicksRemaining", 0);
            Scribe_Values.Look(ref feedbackAttack, "groundFeedbackAttack", HiveLordPlannedAttack.Acid);
            Scribe_Collections.Look(ref feedbackAreaCells, "groundFeedbackCells", LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && feedbackAreaCells == null)
            {
                feedbackAreaCells = new List<IntVec3>();
            }
        }
    }
}
