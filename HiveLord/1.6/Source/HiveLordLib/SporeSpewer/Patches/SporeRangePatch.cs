using HarmonyLib;
using RimWorld;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //在单次攻击有效射程读取时应用孢子倍率，不修改共享武器定义。
    [HarmonyPatch(typeof(Verb), nameof(Verb.EffectiveRange), MethodType.Getter)]
    internal static class SporeRangePatch
    {
        //缩短受影响单位的远程攻击射程，排除近战、建筑与非攻击能力。
        private static void Postfix(Verb __instance, ref float __result)
        {
            VerbProperties props = __instance.verbProps;
            if (props == null || props.IsMeleeAttack || props.range <= 1.5f
                || !SporeDebuffUtility.IsAffected(__instance.CasterPawn)) return;
            IAbilityVerb abilityVerb = __instance as IAbilityVerb;
            if (abilityVerb != null && !abilityVerb.Ability.def.hostile) return;
            if (props.defaultProjectile != null || __instance is Verb_LaunchProjectile
                || __instance is Verb_ShootBeam || __instance is Verb_Spray)
                __result *= 0.5f;
        }
    }
}
