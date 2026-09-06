using System.Linq;
using UnityEngine;

namespace HiveLordLib
{
    //在两段已采样动画之间插值局部骨骼姿势，不依赖Animator自行推进的时间。
    internal sealed class HiveLordPoseBlender
    {
        private readonly Transform[] transforms;
        private readonly Vector3[] positions;
        private readonly Quaternion[] rotations;
        private readonly Vector3[] scales;

        //缓存模型子节点和姿势缓冲，保留由地图投影控制的模型根节点位置。
        internal HiveLordPoseBlender(Animator animator)
        {
            transforms = animator.GetComponentsInChildren<Transform>(true)
                .Where(node => node != animator.transform).ToArray();
            positions = new Vector3[transforms.Length];
            rotations = new Quaternion[transforms.Length];
            scales = new Vector3[transforms.Length];
        }

        //按游戏进度重建起止姿势并混合，暂停和读档不会改变衔接结果。
        internal void Play(Animator animator, HiveLordAnimationCatalog catalog,
            HiveLordVisualState from, float fromProgress, HiveLordVisualState to, float toProgress, float blend)
        {
            blend = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(blend));
            if (blend < 1f)
            {
                catalog.PlayAt(animator, from, Mathf.Min(fromProgress, 0.99999f));
                for (int index = 0; index < transforms.Length; index++)
                {
                    positions[index] = transforms[index].localPosition;
                    rotations[index] = transforms[index].localRotation;
                    scales[index] = transforms[index].localScale;
                }
            }
            catalog.PlayAt(animator, to, Mathf.Min(toProgress, 0.99999f));
            if (blend >= 1f) return;
            for (int index = 0; index < transforms.Length; index++)
            {
                Transform node = transforms[index];
                node.localPosition = Vector3.Lerp(positions[index], node.localPosition, blend);
                node.localRotation = Quaternion.Slerp(rotations[index], node.localRotation, blend);
                node.localScale = Vector3.Lerp(scales[index], node.localScale, blend);
            }
        }
    }
}
