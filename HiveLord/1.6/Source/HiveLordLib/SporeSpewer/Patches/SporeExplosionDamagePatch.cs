using System;
using HarmonyLib;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //记录原版逐目标爆炸结算作用域，在嵌套调用和异常退出时恢复原上下文。
    [HarmonyPatch(typeof(DamageWorker), "ExplosionDamageThing")]
    internal static class SporeExplosionDamagePatch
    {
        [ThreadStatic] private static Thing currentTarget;

        //判断生命伤害是否来自当前目标正在执行的真实爆炸结算。
        internal static bool Allows(Thing target, DamageInfo damage)
        {
            return ReferenceEquals(currentTarget, target) && damage.Def.harmsHealth;
        }

        //在爆炸接触单个目标时保存外层上下文并绑定本次目标。
        private static void Prefix(Thing t, out Thing __state)
        {
            __state = currentTarget;
            currentTarget = t;
        }

        //无论成功还是抛出异常都恢复外层目标，保留原异常供游戏报告。
        private static Exception Finalizer(Exception __exception, Thing __state)
        {
            currentTarget = __state;
            return __exception;
        }
    }
}
