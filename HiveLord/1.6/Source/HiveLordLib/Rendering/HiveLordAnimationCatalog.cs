using Verse;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HiveLordLib
{
    //按 FBX 动画编号解析霸王虫六个视觉状态，并提供播放和时长查询。
    internal sealed class HiveLordAnimationCatalog
    {
        private static readonly Dictionary<HiveLordVisualState, string> AnimationSuffixes =
            new Dictionary<HiveLordVisualState, string>
            {
                { HiveLordVisualState.Underground, "0xbc6a602db7d3b836" },
                { HiveLordVisualState.Emerging, "0xb6f020ce7dfe8c94" },
                { HiveLordVisualState.Idle, "0xdcadb7306e51bb13" },
                { HiveLordVisualState.AcidAttack, "0x147fef00fa031b08" },
                { HiveLordVisualState.GroundSlam, "0x08d751eaf2f696f5" },
                { HiveLordVisualState.Submerging, "0xd3640c424c92f4d9" }
            };

        private readonly Dictionary<HiveLordVisualState, AnimationClip> clips =
            new Dictionary<HiveLordVisualState, AnimationClip>();

        //从 Animator Controller 的动画片段中解析并验证六个必需状态。
        public HiveLordAnimationCatalog(Animator animator)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException("HiveLord_Error_ControllerMissing".Translate().ToString());
            }

            AnimationClip[] availableClips = animator.runtimeAnimatorController.animationClips;
            foreach (KeyValuePair<HiveLordVisualState, string> pair in AnimationSuffixes)
            {
                clips.Add(pair.Key, FindClip(availableClips, pair.Value));
            }
        }

        //为战斗状态机解析每个必需动画片段的实际秒数。
        public static Dictionary<HiveLordVisualState, float> ResolveDurations(AnimationClip[] availableClips)
        {
            Dictionary<HiveLordVisualState, float> durations =
                new Dictionary<HiveLordVisualState, float>();
            foreach (KeyValuePair<HiveLordVisualState, string> pair in AnimationSuffixes)
            {
                durations.Add(pair.Key, FindClip(availableClips, pair.Value).length);
            }

            return durations;
        }

        //立即求值指定动画姿势，循环片段重复播放，出入土等单次片段停在末帧。
        public void PlayAt(Animator animator, HiveLordVisualState state, float normalizedTime)
        {
            AnimationClip clip = clips[state];
            float progress = clip.isLooping ? Mathf.Repeat(normalizedTime, 1f) : Mathf.Clamp01(normalizedTime);
            animator.Play(clip.name, 0, progress);
            animator.Update(0f);
        }

        //返回视觉状态对应动画片段的实际播放时长。
        public float GetDuration(HiveLordVisualState state)
        {
            return Mathf.Max(0.1f, clips[state].length);
        }

        //按不区分大小写的动画编号后缀查找唯一片段。
        private static AnimationClip FindClip(AnimationClip[] availableClips, string suffix)
        {
            AnimationClip result = null;
            for (int index = 0; index < availableClips.Length; index++)
            {
                AnimationClip clip = availableClips[index];
                if (clip == null || !clip.name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (result != null)
                {
                    throw new InvalidOperationException("HiveLord_Error_AnimationAmbiguous".Translate().ToString() + suffix);
                }

                result = clip;
            }

            if (result == null)
            {
                throw new InvalidOperationException("HiveLord_Error_AnimationMissing".Translate().ToString() + suffix);
            }

            return result;
        }
    }
}
