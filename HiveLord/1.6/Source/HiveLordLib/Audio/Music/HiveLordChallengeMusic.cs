using RimWorld.Planet;
using Verse;

namespace HiveLordLib
{
    //根据当前可见地图和持久挑战阶段选择专用背景音乐。
    internal static class HiveLordChallengeMusic
    {
        //仅为有效挑战返回曲目，结束、重试、世界视图和其他地图交还原版选曲。
        internal static SongDef DesiredSong()
        {
            Map map = Find.CurrentMap;
            if (map == null || WorldRendererUtility.WorldSelected
                || !(map.Parent is HiveLordChallengeSite site)) return null;
            HiveLordChallengeMapComponent challenge = map.GetComponent<HiveLordChallengeMapComponent>();
            if (!challenge.Initialized) return null;
            if (site.Phase == HiveLordChallengePhase.ClearingSpores)
                return HiveLordMusicDefOf.HiveLord_Music_Preparation;
            //战斗阶段贯穿每轮出土与遁地，只追踪本次登记目标，不受友方召唤体影响。
            if (site.Phase == HiveLordChallengePhase.Fighting && challenge.Boss != null
                && challenge.Boss.Spawned && !challenge.Boss.IsDying && !challenge.Boss.Destroyed)
                return HiveLordMusicDefOf.HiveLord_Music_Battle;
            return null;
        }
    }
}
