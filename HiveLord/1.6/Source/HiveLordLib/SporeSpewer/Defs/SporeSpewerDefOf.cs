using RimWorld;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //提供孢子喷涌虫实体和地图干扰状态的定义引用。
    [DefOf]
    public static class SporeSpewerDefOf
    {
        public static ThingDef HiveLord_SporeSpewer;
        public static HediffDef HiveLord_SporeInterference;

        //保证首次访问时定义引用完成初始化。
        static SporeSpewerDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(SporeSpewerDefOf));
        }
    }
}
