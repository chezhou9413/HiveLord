using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //保存霸王虫的独立难度倍率与战斗提示开关。
    public sealed class HiveLordSettings : ModSettings
    {
        public const float MinMultiplier = 0.01f;
        public const float MaxMultiplier = 25f;
        public float attackPowerMultiplier = 1f;
        public float incomingDamageMultiplier = 1f;
        public float hitPointMultiplier = 1f;
        public float combatTempoMultiplier = 1f;
        public bool showAttackWarnings = true;

        //恢复全部难度倍率，保留玩家的视觉提示偏好。
        public void ResetDifficulty()
        {
            attackPowerMultiplier = incomingDamageMultiplier = hitPointMultiplier = combatTempoMultiplier = 1f;
        }

        //限制设置倍率范围，保证生命值和阶段时长有效。
        public void Normalize()
        {
            attackPowerMultiplier = Mathf.Clamp(attackPowerMultiplier, MinMultiplier, MaxMultiplier);
            incomingDamageMultiplier = Mathf.Clamp(incomingDamageMultiplier, MinMultiplier, MaxMultiplier);
            hitPointMultiplier = Mathf.Clamp(hitPointMultiplier, MinMultiplier, MaxMultiplier);
            combatTempoMultiplier = Mathf.Clamp(combatTempoMultiplier, MinMultiplier, MaxMultiplier);
        }

        //读写难度倍率和攻击预警显示偏好。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref attackPowerMultiplier, "attackPowerMultiplier", 1f);
            Scribe_Values.Look(ref incomingDamageMultiplier, "incomingDamageMultiplier", 1f);
            Scribe_Values.Look(ref hitPointMultiplier, "hitPointMultiplier", 1f);
            Scribe_Values.Look(ref combatTempoMultiplier, "combatTempoMultiplier", 1f);
            Scribe_Values.Look(ref showAttackWarnings, "showAttackWarnings", true);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Normalize();
            }
        }
    }
}
