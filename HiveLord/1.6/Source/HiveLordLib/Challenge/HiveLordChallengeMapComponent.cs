using System;
using System.Collections.Generic;
using HiveLordLib.SporeSpewer;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //驱动单次挑战的孢子初始化、三分钟倒计时、唯一霸王虫引用及任务界面。
    public sealed class HiveLordChallengeMapComponent : MapComponent
    {
        public const int PreparationTicks = 10800;
        private bool initialized;
        private bool initializationFailed;
        private bool warningSent;
        private int arrivalTick = -1;
        private List<SporeSpewerThing> sources = new List<SporeSpewerThing>();
        private HiveLordProjectionThing boss;
        private HiveLordChallengeSite Site => map.Parent as HiveLordChallengeSite;
        public bool Initialized => initialized;
        public int RemainingTicks => arrivalTick < 0 ? PreparationTicks : Math.Max(0, arrivalTick - Find.TickManager.TicksGame);
        public HiveLordProjectionThing Boss => boss;

        //统计本次生成的五个孢子源，不把调试生成的其他来源计入任务进度。
        public int RemainingSpores
        {
            get
            {
                int count = 0;
                foreach (SporeSpewerThing source in sources)
                    if (source != null && source.Spawned && source.Map == map && !source.Destroyed && source.HitPoints > 0) count++;
                return count;
            }
        }

        //为地图和世界据点共用同一倒计时及挑战进度文案。
        public string StatusText
        {
            get
            {
                if (initializationFailed) return "HiveLord_Challenge_InitFailed".Translate().ToString();
                if (Site.Phase == HiveLordChallengePhase.Completed) return "HiveLord_Challenge_CompleteStatus".Translate(RemainingSpores).ToString();
                if (Site.Phase == HiveLordChallengePhase.Fighting) return "HiveLord_Challenge_FightingStatus".Translate(RemainingSpores).ToString();
                int seconds = (RemainingTicks + 59) / 60;
                string time = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
                return "HiveLord_Challenge_CountdownStatus".Translate(time, RemainingSpores).ToString();
            }
        }

        //由原版自动建立地图组件，普通地图不会启动挑战逻辑。
        public HiveLordChallengeMapComponent(Map map) : base(map) { }

        //在队伍部署完成后只初始化一次，预选全部位置后才生成孢子源。
        internal void InitializeAttempt()
        {
            if (Site == null || initialized || initializationFailed || Site.Phase == HiveLordChallengePhase.Completed) return;
            Pawn entry = HiveLordChallengeParticipants.FindSpawned(map);
            if (entry == null) return;
            try
            {
                List<IntVec3> positions = HiveLordSporePlacement.FindPositions(map, entry);
                foreach (IntVec3 cell in positions) sources.Add(SporeSpewerSpawnUtility.Spawn(map, cell));
                arrivalTick = Find.TickManager.TicksGame + PreparationTicks;
                initialized = true;
                Site.BeginAttempt();
                Find.LetterStack.ReceiveLetter("HiveLord_Challenge_EnteredTitle".Translate().ToString(), "HiveLord_Challenge_EnteredBody".Translate().ToString(),
                    LetterDefOf.ThreatSmall, entry);
            }
            catch (Exception exception)
            {
                foreach (SporeSpewerThing source in sources)
                    if (source != null && !source.Destroyed) source.Destroy(DestroyMode.Vanish);
                sources.Clear();
                ReportInitializationFailure(exception);
            }
        }

        //按游戏时间推进挑战，在非当前地图也保持相同计时和唯一生成语义。
        public override void MapComponentTick()
        {
            HiveLordChallengeSite site = Site;
            if (site == null || initializationFailed || site.Phase == HiveLordChallengePhase.Completed
                || site.Phase == HiveLordChallengePhase.WaitingForRetry) return;
            if (!initialized)
            {
                InitializeAttempt();
                return;
            }
            if (site.Phase != HiveLordChallengePhase.ClearingSpores) return;
            if (!warningSent && RemainingTicks <= 1800)
            {
                warningSent = true;
                Find.LetterStack.ReceiveLetter("HiveLord_Challenge_WarningTitle".Translate().ToString(), "HiveLord_Challenge_WarningBody".Translate().ToString(),
                    LetterDefOf.ThreatBig, site);
            }
            if (RemainingTicks != 0) return;
            Pawn target = HiveLordChallengeParticipants.FindEncounterTarget(map);
            if (target == null)
            {
                site.InterruptAttempt();
                return;
            }
            try
            {
                boss = HiveLordChallengeBossSpawner.Spawn(map, target);
                site.BeginFight();
                Find.LetterStack.ReceiveLetter("HiveLord_Challenge_BossTitle".Translate().ToString(), "HiveLord_Challenge_BossBody".Translate().ToString(),
                    LetterDefOf.ThreatBig, boss);
            }
            catch (Exception exception) { ReportInitializationFailure(exception); }
        }

        //仅接收本次登记目标的真实死亡流程通知，清图和其他霸王虫不计通关。
        internal void NotifyBossDying(HiveLordProjectionThing dyingBoss)
        {
            if (Site != null && Site.Phase == HiveLordChallengePhase.Fighting
                && ReferenceEquals(boss, dyingBoss) && dyingBoss.IsDying) Site.CompleteHunt();
        }

        //在当前挑战地图绘制独立状态提示，避免覆盖霸王虫原有血条。
        public override void MapComponentOnGUI()
        {
            if (Site == null || Find.CurrentMap != map || WorldRendererUtility.WorldSelected
                || Event.current.type != EventType.Repaint || (!initialized && !initializationFailed)) return;
            HiveLordChallengeHud.Draw(this);
        }

        //记录一次初始化异常并给出撤离提示，不重复生成或无限重试。
        private void ReportInitializationFailure(Exception exception)
        {
            initializationFailed = true;
            Site.NotifyInitializationFailed();
            Log.Error("[HiveLord/Challenge] Site initialization failed: " + exception);
            Find.LetterStack.ReceiveLetter("HiveLord_Challenge_FailureTitle".Translate().ToString(), "HiveLord_Challenge_FailureBody".Translate(exception.Message),
                LetterDefOf.NegativeEvent, Site);
        }

        //保存倒计时、生成标记和实例引用，读档后继续本轮而不重新初始化。
        public override void ExposeData()
        {
            Scribe_Values.Look(ref initialized, "challengeInitialized");
            Scribe_Values.Look(ref initializationFailed, "challengeInitializationFailed");
            Scribe_Values.Look(ref warningSent, "arrivalWarningSent");
            Scribe_Values.Look(ref arrivalTick, "bossArrivalTick", -1);
            Scribe_Collections.Look(ref sources, "challengeSporeSources", LookMode.Reference);
            Scribe_References.Look(ref boss, "challengeBoss");
        }
    }
}
