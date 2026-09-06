using Verse;

namespace HiveLordLib.SporeSpewer
{
    //展示地图孢子干扰，并在单位死亡或离开影响地图后自动结束。
    public sealed class Hediff_SporeInterference : Hediff
    {
        //使失去地图孢子源的单位不再保留健康减益。
        public override bool ShouldRemove => !SporeDebuffUtility.IsAffected(pawn);
    }
}
