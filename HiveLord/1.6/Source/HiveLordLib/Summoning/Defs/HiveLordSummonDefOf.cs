using RimWorld;
using Verse;

namespace HiveLordLib
{
    //提供召唤技能、训练器和小型霸王虫的独立定义引用。
    [DefOf]
    public static class HiveLordSummonDefOf
    {
        public static AbilityDef HiveLord_Summon;
        public static ThingDef HiveLord_SummonTrainer;
        public static ThingDef HiveLord_Summoned;
        public static ThingDef HiveLord_SummonedHitProxy;

        //让原版在使用这些字段前完成定义绑定。
        static HiveLordSummonDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HiveLordSummonDefOf));
        }
    }
}
