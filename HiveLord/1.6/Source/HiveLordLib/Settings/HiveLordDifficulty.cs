using Verse;
using System.Linq;
using RimWorld;
using UnityEngine;

namespace HiveLordLib
{
    //把玩家难度设置转换为战斗倍率、最大生命值和非动画阶段时长。
    internal static class HiveLordDifficulty
    {
        private static float baseHitPoints;
        public static float AttackPower => HiveLordMod.Settings.attackPowerMultiplier;
        public static float IncomingDamage => HiveLordMod.Settings.incomingDamageMultiplier;
        public static int MaxHitPoints => Mathf.Max(1, Mathf.RoundToInt(baseHitPoints * HiveLordMod.Settings.hitPointMultiplier));

        //资源加载完成后记录 XML 基础生命值并应用当前设置。
        public static void Initialize()
        {
            baseHitPoints = HiveLordDefOf.HiveLord_HitProxy.statBases.Single(stat => stat.stat == StatDefOf.MaxHitPoints).value;
            ApplyHitPoints();
        }

        //同步原版受击体生命值属性，使检查面板与实际受击上限一致。
        public static void ApplyHitPoints()
        {
            HiveLordMod.Settings.Normalize();
            HiveLordDefOf.HiveLord_HitProxy.statBases.Single(stat => stat.stat == StatDefOf.MaxHitPoints).value = MaxHitPoints;
            HiveLordSummonDefOf.HiveLord_SummonedHitProxy.statBases.Single(stat => stat.stat == StatDefOf.MaxHitPoints).value
                = Mathf.Max(1, Mathf.RoundToInt(MaxHitPoints / 3f));
        }

        //缩放地下等待、换位和攻击间隔，保持动画片段及命中时间不变。
        public static int ScaleInterval(int ticks)
        {
            return ticks == 0 ? 0 : Mathf.Max(1, Mathf.RoundToInt(ticks / HiveLordMod.Settings.combatTempoMultiplier));
        }

        //按沙虫同类权重给出当前倍率的粗略挑战等级。
        public static string GetRating()
        {
            HiveLordSettings settings = HiveLordMod.Settings;
            float score = settings.attackPowerMultiplier * 0.3f + settings.hitPointMultiplier * 0.3f
                + settings.combatTempoMultiplier * 0.2f + 0.2f / settings.incomingDamageMultiplier;
            return score < 0.75f ? "HiveLord_Difficulty_Low".Translate().ToString() : score < 1.5f ? "HiveLord_Difficulty_Standard".Translate().ToString() : score < 3.5f ? "HiveLord_Difficulty_High".Translate().ToString() : score < 8f ? "HiveLord_Difficulty_Extreme".Translate().ToString() : "HiveLord_Difficulty_Catastrophic".Translate().ToString();
        }
    }
}
