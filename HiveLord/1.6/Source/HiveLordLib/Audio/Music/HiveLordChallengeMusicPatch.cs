using HarmonyLib;
using RimWorld;
using Verse;

namespace HiveLordLib
{
    //在原版音乐管理器完成初始化与更新后，及时响应挑战入场和阶段切换。
    [HarmonyPatch(typeof(MusicManagerPlay), nameof(MusicManagerPlay.MusicUpdate))]
    internal static class HiveLordChallengeMusicPatch
    {
        //仅在所需序列尚未生效时重评转换，避免暂停或变速导致等待刻度检查。
        private static void Postfix(MusicManagerPlay __instance)
        {
            if (__instance.disabled) return;
            SongDef desired = HiveLordChallengeMusic.DesiredSong();
            if (desired == null) return;
            MusicSequenceWorker sequence = __instance.MusicSequenceWorker;
            if (sequence is MusicSequenceWorker_HiveLordChallenge && sequence.def.song == desired) return;
            __instance.CheckTransitions();
        }
    }
}
