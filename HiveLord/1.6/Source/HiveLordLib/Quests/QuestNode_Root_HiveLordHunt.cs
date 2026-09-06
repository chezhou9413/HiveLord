using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using Verse;

namespace HiveLordLib
{
    //构建独立辛迪加委托的内容、接受条件及世界据点关联部件。
    public sealed class QuestNode_Root_HiveLordHunt : QuestNode
    {
        //创建永久待接受的狩猎任务并传入已选定的世界坐标。
        protected override void RunInt()
        {
            Quest quest = QuestGen.quest;
            quest.name = HiveLordQuestUtility.Title;
            quest.description = HiveLordQuestUtility.Description;
            //原版在根节点结束后还会解析名称和描述，预先提供解析结果以保留完整任务文案。
            QuestGen.slate.Set("resolvedQuestName", HiveLordQuestUtility.Title);
            QuestGen.slate.Set("resolvedQuestDescription", HiveLordQuestUtility.Description);
            quest.challengeRating = 4;
            quest.tags.Add(HiveLordQuestUtility.QuestTag);
            PlanetTile candidate;
            if (!QuestGen.slate.TryGet("hiveLordSiteTile", out candidate)) candidate = PlanetTile.Invalid;
            quest.AddPart(new QuestPart_HiveLordHunt
            {
                CandidateTile = candidate,
                signalListenMode = QuestPart.SignalListenMode.OngoingOrNotYetAccepted
            });
        }

        //仅允许存在玩家殖民地且没有阻塞委托时生成任务。
        protected override bool TestRunInt(Slate slate)
        {
            return Find.AnyPlayerHomeMap != null && !HiveLordQuestUtility.HasBlockingQuest();
        }
    }
}
