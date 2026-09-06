using System.Collections.Generic;
using Verse;

namespace HiveLordLib
{
    //承载 ThingDef 中可调整的霸王虫锁敌、位置、时序与伤害参数。
    public sealed class HiveLordCombatExtension : DefModExtension
    {
        public int targetSearchIntervalTicks = 60;
        public float targetSearchRadius = 10000f;
        public float targetRetentionRadius = 10000f;
        public float acidAttackChance = 0.6f;
        public float acidEmergenceMinRadius = 20f;
        public float acidEmergenceMaxRadius = 30f;
        public float acidMaxRange = 55f;
        public float slamEmergenceMinRadius = 6f;
        public float slamEmergenceMaxRadius = 10f;
        public float slamMaxRange = 16f;
        public int surfaceIdleTicks = 150;
        public int undergroundTravelTicks = 360;
        public int undergroundRestMinTicks = 180;
        public int undergroundRestMaxTicks = 480;
        public int attackRecoveryTicks = 27;
        public int submergingHideDelayTicks = 18;
        public int groundFeedbackTicks = 150;
        public int acidGroundDurationTicks = 720;
        public float emergenceShockwaveNormalizedTime = 0.68f;
        public float emergencePushRadius = 24f;
        public float emergencePushMinDistance = 16f;
        public float emergencePushMaxDistance = 28f;
        public float emergenceShockwaveScale = 14f;
        public float emergenceExitNormalizedTime = 1f;
        public float submergingExitNormalizedTime = 1f;
        public float acidImpactNormalizedTime = 0.6f;
        public float slamImpactNormalizedTime = 0.4f;
        public float acidEmissionStartNormalizedTime = 0.42f;
        public float acidEmissionEndNormalizedTime = 0.68f;
        public float attackExitNormalizedTime = 0.9f;
        public float emergenceShakeMagnitude = 0.09f;
        public float acidShakeMagnitude = 0.055f;
        public float slamShakeMagnitude = 0.14f;
        public float submergingShakeMagnitude = 0.065f;
        public float screenShakeDistance = 85f;
        public float acidAreaStartOffset = 4f;
        public float acidAreaLength = 60f;
        public float acidAreaNearHalfWidth = 8f;
        public float acidAreaFarHalfWidth = 18f;
        public float acidDamage = 60f;
        public float acidArmorPenetration = 0.35f;
        public float slamAreaBackLength = 16f;
        public float slamAreaForwardLength = 52f;
        public float slamAreaHalfWidth = 16f;
        public float slamDamage = 45f;
        public float slamArmorPenetration = 0.35f;

        //复制并缩放所有攻击距离、区域和出土冲击，保留全图搜索及原有伤害和节奏。
        internal HiveLordCombatExtension WithRangeScale(float scale)
        {
            var result = (HiveLordCombatExtension)MemberwiseClone();
            result.acidEmergenceMinRadius *= scale;
            result.acidEmergenceMaxRadius *= scale;
            result.acidMaxRange *= scale;
            result.slamEmergenceMinRadius *= scale;
            result.slamEmergenceMaxRadius *= scale;
            result.slamMaxRange *= scale;
            result.emergencePushRadius *= scale;
            result.emergencePushMinDistance *= scale;
            result.emergencePushMaxDistance *= scale;
            result.emergenceShockwaveScale *= scale;
            result.acidAreaStartOffset *= scale;
            result.acidAreaLength *= scale;
            result.acidAreaNearHalfWidth *= scale;
            result.acidAreaFarHalfWidth *= scale;
            result.slamAreaBackLength *= scale;
            result.slamAreaForwardLength *= scale;
            result.slamAreaHalfWidth *= scale;
            result.emergenceShakeMagnitude *= scale;
            result.acidShakeMagnitude *= scale;
            result.slamShakeMagnitude *= scale;
            result.submergingShakeMagnitude *= scale;
            return result;
        }

        //在 Def 加载时拒绝会破坏战斗状态机时序或范围判定的配置。
        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (targetSearchIntervalTicks <= 0)
            {
                yield return "HiveLord_Error_SearchInterval".Translate().ToString();
            }

            if (targetSearchRadius <= 0f || targetRetentionRadius < targetSearchRadius)
            {
                yield return "HiveLord_Error_SearchRadii".Translate().ToString();
            }

            if (acidAttackChance < 0f || acidAttackChance > 1f)
            {
                yield return "HiveLord_Error_AttackChance".Translate().ToString();
            }

            if (!IsValidRing(acidEmergenceMinRadius, acidEmergenceMaxRadius)
                || !IsValidRing(slamEmergenceMinRadius, slamEmergenceMaxRadius))
            {
                yield return "HiveLord_Error_EmergenceRing".Translate().ToString();
            }

            if (surfaceIdleTicks <= 0
                || undergroundTravelTicks <= 0
                || undergroundRestMinTicks < 0
                || undergroundRestMaxTicks < undergroundRestMinTicks
                || attackRecoveryTicks <= 0
                || submergingHideDelayTicks < 0
                || groundFeedbackTicks <= 0
                || acidGroundDurationTicks < 600)
            {
                yield return "HiveLord_Error_PhaseTiming".Translate().ToString();
            }

            if (!IsNormalizedTime(emergenceExitNormalizedTime)
                || !IsNormalizedTime(submergingExitNormalizedTime)
                || !IsNormalizedTime(emergenceShockwaveNormalizedTime)
                || !IsNormalizedTime(acidImpactNormalizedTime)
                || !IsNormalizedTime(slamImpactNormalizedTime)
                || !IsNormalizedTime(acidEmissionStartNormalizedTime)
                || !IsNormalizedTime(acidEmissionEndNormalizedTime)
                || !IsNormalizedTime(attackExitNormalizedTime)
                || acidImpactNormalizedTime >= attackExitNormalizedTime
                || slamImpactNormalizedTime >= attackExitNormalizedTime
                || acidEmissionStartNormalizedTime >= acidImpactNormalizedTime
                || acidImpactNormalizedTime >= acidEmissionEndNormalizedTime)
            {
                yield return "HiveLord_Error_AnimationTiming".Translate().ToString();
            }

            if (emergencePushRadius <= 0f
                || emergencePushRadius > GenRadial.MaxRadialPatternRadius
                || emergencePushMinDistance <= 0f
                || emergencePushMaxDistance < emergencePushMinDistance
                || emergenceShockwaveScale <= 0f)
            {
                yield return "HiveLord_Error_ShockwaveGeometry".Translate().ToString();
            }

            if (acidAreaStartOffset < 0f
                || acidAreaLength <= acidAreaStartOffset
                || acidAreaNearHalfWidth <= 0f
                || acidAreaFarHalfWidth < acidAreaNearHalfWidth
                || slamAreaBackLength < 0f
                || slamAreaForwardLength <= 0f
                || slamAreaHalfWidth <= 0f)
            {
                yield return "HiveLord_Error_AttackGeometry".Translate().ToString();
            }

            if (!IsValidShakeMagnitude(emergenceShakeMagnitude)
                || !IsValidShakeMagnitude(acidShakeMagnitude)
                || !IsValidShakeMagnitude(slamShakeMagnitude)
                || !IsValidShakeMagnitude(submergingShakeMagnitude)
                || screenShakeDistance <= 0f)
            {
                yield return "HiveLord_Error_Shake".Translate().ToString();
            }
        }

        //判断出土环形范围是否可以交给径向单元枚举器处理。
        private static bool IsValidRing(float minimum, float maximum)
        {
            return minimum >= 0f && maximum >= minimum && maximum <= GenRadial.MaxRadialPatternRadius;
        }

        //判断动画阶段使用的归一化时间是否有效。
        private static bool IsNormalizedTime(float value)
        {
            return value > 0f && value <= 1f;
        }

        //限制震动强度不超过原版 CameraShaker 的最大设计幅度。
        private static bool IsValidShakeMagnitude(float value)
        {
            return value >= 0f && value <= 0.2f;
        }
    }
}
