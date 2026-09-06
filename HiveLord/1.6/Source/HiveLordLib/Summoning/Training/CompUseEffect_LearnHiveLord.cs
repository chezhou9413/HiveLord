using System;
using RimWorld;
using Verse;

namespace HiveLordLib
{
    //通过一次性训练器永久教授召唤霸王虫，不要求灵能链接或额外资料片。
    public sealed class CompUseEffect_LearnHiveLord : CompUseEffect
    {
        //限制为具备能力追踪器的类人生物，并阻止重复学习浪费训练器。
        public override AcceptanceReport CanBeUsedBy(Pawn pawn)
        {
            if (!pawn.RaceProps.Humanlike || pawn.abilities == null) return "HiveLord_Summon_UnsupportedUser".Translate().ToString();
            if (pawn.abilities.GetAbility(HiveLordSummonDefOf.HiveLord_Summon, true) != null)
                return "HiveLord_Summon_AlreadyKnown".Translate().ToString();
            return base.CanBeUsedBy(pawn);
        }

        //教授永久技能，消耗训练器由原版自毁组件处理。
        public override void DoEffect(Pawn usedBy)
        {
            AcceptanceReport report = CanBeUsedBy(usedBy);
            if (!report.Accepted) throw new InvalidOperationException(report.Reason);
            base.DoEffect(usedBy);
            usedBy.abilities.GainAbility(HiveLordSummonDefOf.HiveLord_Summon);
            Messages.Message("HiveLord_Summon_Learned".Translate(usedBy.LabelShort), usedBy, MessageTypeDefOf.PositiveEvent);
        }
    }
}
