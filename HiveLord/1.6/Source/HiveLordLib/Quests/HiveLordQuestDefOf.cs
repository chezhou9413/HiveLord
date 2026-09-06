using RimWorld;
using Verse;

namespace HiveLordLib
{
    //提供独立辛迪加委托和世界挑战据点的定义引用。
    [DefOf]
    public static class HiveLordQuestDefOf
    {
        public static QuestScriptDef HiveLord_SyndicateHunt;
        public static WorldObjectDef HiveLord_ChallengeSite;

        //在首次使用前初始化定义引用。
        static HiveLordQuestDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HiveLordQuestDefOf));
        }
    }
}
