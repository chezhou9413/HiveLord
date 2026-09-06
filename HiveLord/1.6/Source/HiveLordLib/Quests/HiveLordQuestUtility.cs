using RimWorld;
using RimWorld.QuestGen;
using Verse;

namespace HiveLordLib
{
    //公开委托发放接口，集中提供任务文案与重复任务判定。
    public static class HiveLordQuestUtility
    {
        internal const string QuestTag = "HiveLord_SyndicateHunt";
        //提供当前语言的委托标题。
        internal static string Title => "HiveLord_Quest_Title".Translate().ToString();

        //把报酬填入完整委托文本，保持语序由语言资源决定。
        internal static string Description => "HiveLord_Quest_Description".Translate(HiveLordHuntRewards.Description).ToString();

        //判断现有待接受、进行中或已成功的委托是否阻止重复发放。
        internal static bool HasBlockingQuest()
        {
            foreach (Quest quest in Find.QuestManager.QuestsListForReading)
                if (quest.tags.Contains(QuestTag) && (quest.State == QuestState.NotYetAccepted
                    || quest.State == QuestState.Ongoing || quest.State == QuestState.EndedSuccess)) return true;
            return false;
        }

        //在满足财富和选址条件时生成唯一委托，并向玩家发送可接受通知。
        public static bool TryOfferQuest()
        {
            if (HasBlockingQuest() || Current.Game.GetComponent<HiveLordQuestGameComponent>().Completed) return false;
            float wealth = 0f;
            Map home = null;
            foreach (Map map in Find.Maps)
                if (map.IsPlayerHome)
                {
                    wealth += map.wealthWatcher.WealthTotal;
                    if (home == null) home = map;
                }
            if (home == null || wealth < 500000f || !HiveLordSiteFinder.TryFind(out var tile)) return false;
            var slate = new Slate();
            slate.Set("map", home);
            slate.Set("points", StorytellerUtility.DefaultThreatPointsNow(home));
            slate.Set("hiveLordSiteTile", tile);
            Quest quest = QuestUtility.GenerateQuestAndMakeAvailable(HiveLordQuestDefOf.HiveLord_SyndicateHunt, slate);
            if (quest == null) throw new System.InvalidOperationException("HiveLord_Error_QuestGeneration".Translate().ToString());
            QuestUtility.SendLetterQuestAvailable(quest, Title);
            return true;
        }
    }
}
