using RimWorld;
using Verse;

namespace HiveLordLib
{
    //提供挑战准备和霸王虫战斗曲目的强类型定义引用。
    [DefOf]
    public static class HiveLordMusicDefOf
    {
        public static SongDef HiveLord_Music_Preparation;
        public static SongDef HiveLord_Music_Battle;

        //在定义加载后绑定挑战曲目。
        static HiveLordMusicDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(HiveLordMusicDefOf));
        }
    }
}
