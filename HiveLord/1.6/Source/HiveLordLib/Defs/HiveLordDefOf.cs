using RimWorld;
using Verse;

namespace HiveLordLib
{
    //提供霸王虫运行时创建投影实体与受击代理所需的强类型定义引用。
    [DefOf]
    public static class HiveLordDefOf
    {
        public static ThingDef HiveLord_Projection;
        public static ThingDef HiveLord_HitProxy;

        //要求 RimWorld 在类型初始化时填充全部静态 Def 字段。
        static HiveLordDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HiveLordDefOf));
        }
    }
}
