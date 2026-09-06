using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace HiveLordLib
{
    //维护辛迪加世界据点、单次挑战阶段、重试地图清理和唯一任务结算。
    public sealed class HiveLordChallengeSite : MapParent
    {
        public static readonly IntVec3 ChallengeMapSize = new IntVec3(250, 1, 250);
        private Quest huntQuest;
        private HiveLordChallengePhase phase;
        private int attempts;
        private bool removeMapForRetry;

        public HiveLordChallengePhase Phase => phase;
        public int Attempts => attempts;
        public override MapGeneratorDef MapGeneratorDef => MapGeneratorDefOf.Encounter;
        public override AcceptanceReport CanBeSettled => "HiveLord_Site_CannotSettle".Translate().ToString();
        protected override bool UseGenericEnterMapFloatMenuOption => false;

        //将世界据点绑定到唯一委托，保持不同任务的目标信号彼此独立。
        internal void BindQuest(Quest quest)
        {
            huntQuest = quest;
            questTags = new List<string> { "HiveLordHunt." + quest.id };
            SetFaction(Faction.OfInsects);
        }

        //队伍部署后初始化本次地图，增援进入不会重新生成目标。
        internal void NotifyEntered()
        {
            if (phase == HiveLordChallengePhase.Completed || !HasMap) return;
            //重试生成的新地图先恢复待进入阶段，让容器乘员稍后落地时也能启动初始化。
            if (phase == HiveLordChallengePhase.WaitingForRetry) phase = HiveLordChallengePhase.PendingEntry;
            Map.GetComponent<HiveLordChallengeMapComponent>().InitializeAttempt();
        }

        //记录一轮完成孢子源初始化的挑战并开始清理阶段。
        internal void BeginAttempt()
        {
            attempts++;
            removeMapForRetry = false;
            phase = HiveLordChallengePhase.ClearingSpores;
        }

        //记录本次登记的霸王虫已经进入战斗。
        internal void BeginFight()
        {
            phase = HiveLordChallengePhase.Fighting;
        }

        //初始化失败时保留队伍撤离时间，地图无人后才清理重试。
        internal void NotifyInitializationFailed()
        {
            phase = HiveLordChallengePhase.WaitingForRetry;
            removeMapForRetry = false;
        }

        //中断本次挑战并安排在世界更新阶段清图，保留委托和据点。
        internal void InterruptAttempt()
        {
            if (phase != HiveLordChallengePhase.ClearingSpores && phase != HiveLordChallengePhase.Fighting) return;
            phase = HiveLordChallengePhase.WaitingForRetry;
            removeMapForRetry = true;
            Find.LetterStack.ReceiveLetter("HiveLord_Quest_InterruptedTitle".Translate().ToString(), "HiveLord_Quest_InterruptedBody".Translate().ToString(),
                LetterDefOf.NeutralEvent, this);
        }

        //只在登记的霸王虫死亡后完成当前委托，不销毁尚有队伍停留的地图。
        internal void CompleteHunt()
        {
            if (phase != HiveLordChallengePhase.Fighting) return;
            phase = HiveLordChallengePhase.Completed;
            Current.Game.GetComponent<HiveLordQuestGameComponent>().MarkCompleted();
            HiveLordHuntRewards.Deliver(Map);
            if (huntQuest.State == QuestState.Ongoing) huntQuest.End(QuestEndOutcome.Success, sendLetter: false);
            Find.LetterStack.ReceiveLetter("HiveLord_Quest_CompletedTitle".Translate().ToString(),
                "HiveLord_Quest_CompletedBody".Translate(HiveLordHuntRewards.Description),
                LetterDefOf.PositiveEvent, this);
        }

        //定期检查撤离与团灭，并让原版在安全的世界更新阶段移除地图。
        protected override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (!HasMap || !this.IsHashIntervalTick(250)) return;
            if ((phase == HiveLordChallengePhase.ClearingSpores || phase == HiveLordChallengePhase.Fighting)
                && !HiveLordChallengeParticipants.HasActive(Map)) InterruptAttempt();
        }

        //失败仅移除地图，成功全员离开后同时移除据点。
        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            alsoRemoveWorldObject = phase == HiveLordChallengePhase.Completed;
            if (!HasMap) return false;
            if (removeMapForRetry) return true;
            if (phase == HiveLordChallengePhase.Completed || phase == HiveLordChallengePhase.WaitingForRetry)
                return !HiveLordChallengeParticipants.HasLivingPlayerParty(Map);
            return false;
        }

        //地图移除后保留失败据点的重试状态，并解除待移除标记。
        public override void Notify_MyMapRemoved(Map removedMap)
        {
            base.Notify_MyMapRemoved(removedMap);
            removeMapForRetry = false;
            if (phase != HiveLordChallengePhase.Completed) phase = HiveLordChallengePhase.WaitingForRetry;
        }

        //提供世界地图右键进入本据点的专用到达动作。
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            foreach (FloatMenuOption option in base.GetFloatMenuOptions(caravan)) yield return option;
            foreach (FloatMenuOption option in CaravanArrivalAction_EnterHiveLordSite.GetFloatMenuOptions(caravan, this)) yield return option;
        }

        //展示据点阶段、挑战次数及运行中的倒计时。
        public override string GetInspectString()
        {
            string text = "HiveLord_Site_Inspect".Translate(attempts).ToString();
            if (phase == HiveLordChallengePhase.PendingEntry) return text + "\n" + "HiveLord_Site_Pending".Translate();
            if (phase == HiveLordChallengePhase.WaitingForRetry) return text + "\n" + "HiveLord_Site_Retry".Translate();
            if (phase == HiveLordChallengePhase.Completed) return text + "\n" + "HiveLord_Site_Completed".Translate();
            return text + "\n" + Map.GetComponent<HiveLordChallengeMapComponent>().StatusText;
        }

        //持久保存任务关联、挑战阶段和尝试次数。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref huntQuest, "huntQuest");
            Scribe_Values.Look(ref phase, "challengePhase");
            Scribe_Values.Look(ref attempts, "challengeAttempts");
            Scribe_Values.Look(ref removeMapForRetry, "removeMapForRetry");
        }
    }
}
