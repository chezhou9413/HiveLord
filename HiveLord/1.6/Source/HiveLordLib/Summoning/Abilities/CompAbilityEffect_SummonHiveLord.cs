using System.Linq;
using RimWorld;
using Verse;

namespace HiveLordLib
{
    //将原版技能目标选择、预览和效果执行连接到霸王虫召唤接口。
    public sealed class CompAbilityEffect_SummonHiveLord : CompAbilityEffect
    {
        //验证目标地面可以容纳召唤体，并在需要时显示明确拒绝原因。
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            AcceptanceReport report = HiveLordSummonUtility.CanSpawn(parent.pawn, target.Cell);
            if (!report.Accepted && throwMessages)
                Messages.Message(report.Reason, MessageTypeDefOf.RejectInput, false);
            return report.Accepted;
        }

        //执行一次召唤，不改变技能范围内其他目标或阵营。
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            HiveLordSummonUtility.Spawn(parent.pawn, target.Cell);
        }

        //在技能瞄准期间显示小型受击体的占地边界。
        public override void DrawEffectPreview(LocalTargetInfo target)
        {
            if (!target.IsValid || !parent.pawn.Spawned) return;
            GenDraw.DrawFieldEdges(CellRect.CenteredOn(target.Cell,
                HiveLordSummonDefOf.HiveLord_SummonedHitProxy.size).Cells.ToList());
        }
    }
}
