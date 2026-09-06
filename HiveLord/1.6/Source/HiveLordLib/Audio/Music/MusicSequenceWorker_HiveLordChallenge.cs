using RimWorld;

namespace HiveLordLib
{
    //使用原版无间隔循环和音量控制，在挑战阶段失效时退出音乐序列。
    public sealed class MusicSequenceWorker_HiveLordChallenge : MusicSequenceWorker
    {
        //逐帧检查场景归属，暂停中切图或离开挑战也能及时淡出。
        public override bool ShouldEnd()
        {
            return HiveLordChallengeMusic.DesiredSong() != def.song;
        }
    }
}
