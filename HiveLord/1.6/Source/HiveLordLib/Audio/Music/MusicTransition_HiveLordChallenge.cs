using RimWorld;

namespace HiveLordLib
{
    //把挑战阶段匹配到原版音乐转换条件，避免普通地图随机播放专用曲目。
    public sealed class MusicTransition_HiveLordChallenge : MusicTransition
    {
        //只有当前场景需要本序列曲目时才申请接管音乐。
        public override bool IsTransitionSatisfied()
        {
            return HiveLordChallengeMusic.DesiredSong() == def.sequence.song;
        }
    }
}
