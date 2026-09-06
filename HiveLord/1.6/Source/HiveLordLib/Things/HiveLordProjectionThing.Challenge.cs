using System;
using Verse;

namespace HiveLordLib
{
    //向任务系统提供霸王虫立即登场的公共入口，保持内部战斗状态机封装。
    public sealed partial class HiveLordProjectionThing
    {
        //让已生成且存活的霸王虫对指定玩家单位开启首轮出土战斗。
        public void BeginChallengeEncounter(Pawn target)
        {
            if (!Spawned || IsDying) throw new InvalidOperationException("HiveLord_Error_ChallengeTargetState".Translate().ToString());
            CombatController.BeginChallengeEncounter(target);
            SynchronizeHitProxy();
        }
    }
}
