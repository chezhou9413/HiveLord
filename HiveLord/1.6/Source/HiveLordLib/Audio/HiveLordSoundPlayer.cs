using Verse;
using Verse.Sound;

namespace HiveLordLib
{
    //把霸王虫战斗阶段映射到空间化 SoundDef，并统一从当前地图锚点播放一次性音效。
    internal static class HiveLordSoundPlayer
    {
        //在地下换位预警开始时播放低沉钻行备选音效。
        public static void PlayUndergroundTravel(HiveLordProjectionThing owner)
        {
            Play(owner, HiveLordSoundDefOf.HiveLord_UndergroundTravel);
        }

        //在钻回地下后的随机等待期播放覆盖整个间隔的超低频地下活动声。
        public static void PlayUndergroundPresence(HiveLordProjectionThing owner)
        {
            Play(owner, HiveLordSoundDefOf.HiveLord_UndergroundPresence);
        }

        //在出土动画开始时播放完整破土钻出备选音效。
        public static void PlayEmergence(HiveLordProjectionThing owner)
        {
            Play(owner, HiveLordSoundDefOf.HiveLord_Emergence);
        }

        //在破土冲击波关键帧叠加短促的重型撞击音效。
        public static void PlayEmergenceImpact(HiveLordProjectionThing owner)
        {
            Play(owner, HiveLordSoundDefOf.HiveLord_EmergenceImpact);
        }

        //按计划攻击选择高亢吐酸怒吼或低沉砸地怒吼，覆盖地表预警与攻击起手。
        public static void PlayAttackWarning(
            HiveLordProjectionThing owner,
            HiveLordPlannedAttack plannedAttack)
        {
            SoundDef sound = plannedAttack == HiveLordPlannedAttack.Acid
                ? HiveLordSoundDefOf.HiveLord_AcidRoar
                : HiveLordSoundDefOf.HiveLord_GroundSlamRoar;
            Play(owner, sound);
        }

        //在砸地实际伤害帧播放与冲击特效同步的重击备选音效。
        public static void PlayGroundSlamImpact(HiveLordProjectionThing owner)
        {
            Play(owner, HiveLordSoundDefOf.HiveLord_GroundSlamImpact);
        }

        //在钻回地下动画开始时播放降调钻行音效。
        public static void PlaySubmerging(HiveLordProjectionThing owner)
        {
            Play(owner, HiveLordSoundDefOf.HiveLord_Submerging);
        }

        //验证地图锚点后按当前位置播放一次可距离衰减的地图音效。
        private static void Play(HiveLordProjectionThing owner, SoundDef sound)
        {
            if (!owner.Spawned)
            {
                return;
            }

            sound.PlayOneShot(new TargetInfo(owner.Position, owner.Map));
        }
    }
}
