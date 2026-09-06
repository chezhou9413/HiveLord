using Verse;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiveLordLib
{
    //从捕获预制体的实际 AnimationClip 计算战斗状态机使用的 Tick 时长。
    internal static class HiveLordCombatTiming
    {
        private const float TicksPerSecond = 60f;
        private static Dictionary<HiveLordVisualState, float> clipDurations;

        //返回指定动画播放到某个归一化位置所需的游戏 Tick 数。
        public static int GetDurationTicks(HiveLordVisualState state, float normalizedTime = 1f)
        {
            EnsureDurations();
            return Mathf.Max(1, Mathf.RoundToInt(clipDurations[state] * TicksPerSecond * normalizedTime));
        }

        //返回指定视觉状态对应 AnimationClip 的实际秒数。
        public static float GetDurationSeconds(HiveLordVisualState state)
        {
            EnsureDurations();
            return clipDurations[state];
        }

        //首次使用时从 ChezhouLib 挂载的捕获预制体解析全部必需片段。
        private static void EnsureDurations()
        {
            if (clipDurations != null)
            {
                return;
            }

            GameObject capturePrefab = HiveLordAssets.RequireCapturePrefab();
            Animator animator = capturePrefab.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("HiveLord_Error_CombatTiming".Translate().ToString());
            }

            clipDurations = HiveLordAnimationCatalog.ResolveDurations(
                animator.runtimeAnimatorController.animationClips);
        }
    }
}
