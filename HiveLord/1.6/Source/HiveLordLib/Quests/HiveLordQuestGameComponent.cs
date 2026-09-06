using Verse;

namespace HiveLordLib
{
    //周期检查委托资格，并永久保存本存档已完成霸王虫狩猎的状态。
    public sealed class HiveLordQuestGameComponent : GameComponent
    {
        private bool completed;
        public bool Completed => completed;

        //供原版游戏组件系统创建独立委托记录。
        public HiveLordQuestGameComponent(Game game) { }

        //每六百刻检查一次财富与附近地形，不重复创建已有委托。
        public override void GameComponentTick()
        {
            if (!completed && Find.TickManager.TicksGame > 0 && Find.TickManager.TicksGame % 600 == 0)
                HiveLordQuestUtility.TryOfferQuest();
        }

        //登记成功通关，阻止任务历史清理后再次发放。
        internal void MarkCompleted()
        {
            completed = true;
        }

        //保存通关状态。
        public override void ExposeData()
        {
            Scribe_Values.Look(ref completed, "hiveLordHuntCompleted");
        }
    }
}
