using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //在原版最终生命伤害结算处限制霸王虫单次扣血，不影响其他受击对象。
    [HarmonyPatch(typeof(DamageWorker), nameof(DamageWorker.Apply))]
    internal static class HiveLordDamageCapPatch
    {
        private const float MaximumDamagePerHit = 100f;

        //在伤害倍率计算完成、取整扣血之前插入霸王虫专属伤害上限。
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo roundDamage = AccessTools.Method(typeof(GenMath), nameof(GenMath.RoundRandom),
                new[] { typeof(float) });
            MethodInfo clampDamage = AccessTools.Method(typeof(HiveLordDamageCapPatch), nameof(ClampDamage));
            int matches = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(roundDamage))
                {
                    //栈顶是全部倍率结算后的伤害，补入受击对象后仅对霸王虫封顶。
                    var loadVictim = new CodeInstruction(OpCodes.Ldarg_2);
                    loadVictim.labels.AddRange(instruction.labels);
                    instruction.labels.Clear();
                    yield return loadVictim;
                    yield return new CodeInstruction(OpCodes.Call, clampDamage);
                    matches++;
                }
                yield return instruction;
            }
            if (matches != 1)
                throw new InvalidOperationException("HiveLord_Error_DamageCapPatch".Translate().ToString());
        }

        //将霸王虫最终单次生命伤害限制为100点，保留较小伤害和其他目标的数值。
        private static float ClampDamage(float amount, Thing victim)
        {
            return victim is HiveLordHitProxy ? Mathf.Min(amount, MaximumDamagePerHit) : amount;
        }
    }
}
