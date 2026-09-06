using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace HiveLordLib
{
    //在接受委托时创建据点，并持久保存任务地图的观察目标。
    public sealed class QuestPart_HiveLordHunt : QuestPart_RequirementsToAccept
    {
        internal PlanetTile CandidateTile = PlanetTile.Invalid;
        private HiveLordChallengeSite site;
        private int nextSearchTick;
        private bool candidateValidated;

        //让任务界面可以跳转至本委托对应的世界据点。
        public override IEnumerable<GlobalTargetInfo> QuestLookTargets
        {
            get
            {
                if (site != null && !site.Destroyed) yield return site;
            }
        }

        //接受前重新检查坐标是否仍可用，失效时按原范围重新选址。
        public override AcceptanceReport CanAccept()
        {
            if (Find.AnyPlayerHomeMap == null) return "HiveLord_Quest_NeedHome".Translate().ToString();
            if (Find.TickManager.TicksGame >= nextSearchTick)
            {
                nextSearchTick = Find.TickManager.TicksGame + 600;
                candidateValidated = HiveLordSiteFinder.IsAvailable(CandidateTile)
                    || HiveLordSiteFinder.TryFind(out CandidateTile);
            }
            if (candidateValidated && HiveLordSiteFinder.IsCandidate(CandidateTile)) return true;
            return "HiveLord_Quest_NoSite".Translate().ToString();
        }

        //接受时重新核验路线、准备干旱平原并创建唯一世界据点。
        public override void PreQuestAccept()
        {
            if (site != null) return;
            nextSearchTick = 0;
            AcceptanceReport report = CanAccept();
            if (!report.Accepted) throw new InvalidOperationException(report.Reason);
            HiveLordSiteTerrain.Prepare(CandidateTile);
            site = (HiveLordChallengeSite)WorldObjectMaker.MakeWorldObject(HiveLordQuestDefOf.HiveLord_ChallengeSite);
            site.Tile = CandidateTile;
            site.BindQuest(quest);
            Find.WorldObjects.Add(site);
            Find.LetterStack.ReceiveLetter("HiveLord_Quest_CoordinatesTitle".Translate().ToString(), "HiveLord_Quest_CoordinatesBody".Translate().ToString(),
                LetterDefOf.NeutralEvent, site, quest: quest);
        }

        //保存候选世界坐标和已生成的据点引用。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref CandidateTile, "candidateTile", PlanetTile.Invalid);
            Scribe_References.Look(ref site, "challengeSite");
        }
    }
}
