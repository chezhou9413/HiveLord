using RimWorld;
using Verse;
using Verse.Sound;

namespace HiveLordLib
{
    //提供霸王虫各战斗阶段使用的强类型 SoundDef 引用。
    [DefOf]
    public static class HiveLordSoundDefOf
    {
        public static SoundDef HiveLord_UndergroundTravel;
        public static SoundDef HiveLord_UndergroundPresence;
        public static SoundDef HiveLord_Emergence;
        public static SoundDef HiveLord_EmergenceImpact;
        public static SoundDef HiveLord_AcidRoar;
        public static SoundDef HiveLord_GroundSlamRoar;
        public static SoundDef HiveLord_GroundSlamImpact;
        public static SoundDef HiveLord_Submerging;

        //要求 RimWorld 在静态初始化阶段填充全部霸王虫音效定义。
        static HiveLordSoundDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HiveLordSoundDefOf));
        }
    }
}
