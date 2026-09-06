using RimWorld;
using Verse;

namespace HiveLordLib
{
    //在原版扣除技能冷却前复核召唤位置，避免施法期间被占位仍消耗冷却。
    public sealed class Ability_SummonHiveLord : Ability
    {
        //供原版存档反序列化创建技能实例。
        public Ability_SummonHiveLord() { }

        //供原版能力追踪器绑定技能使用者。
        public Ability_SummonHiveLord(Pawn pawn) : base(pawn) { }

        //供技能工厂同时绑定使用者和能力定义。
        public Ability_SummonHiveLord(Pawn pawn, AbilityDef def) : base(pawn, def) { }

        //仅在目标仍合法时开始原版技能结算和十五日冷却。
        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            AcceptanceReport report = HiveLordSummonUtility.CanSpawn(pawn, target.Cell);
            if (report.Accepted) return base.Activate(target, dest);
            Messages.Message(report.Reason, MessageTypeDefOf.RejectInput, false);
            return false;
        }
    }
}
