using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace HiveLordLib
{
    //提供远行队抵达孢雾巢域后的地图生成、边缘入场和挑战初始化。
    public sealed class CaravanArrivalAction_EnterHiveLordSite : CaravanArrivalAction
    {
        private HiveLordChallengeSite site;
        public override string Label => "HiveLord_Site_Enter".Translate().ToString();
        public override string ReportString => "HiveLord_Site_Entering".Translate().ToString();

        //供原版存档反序列化创建到达动作。
        public CaravanArrivalAction_EnterHiveLordSite() { }

        //绑定远行队准备进入的世界据点。
        public CaravanArrivalAction_EnterHiveLordSite(HiveLordChallengeSite site)
        {
            this.site = site;
        }

        //在旅行期间确认目标坐标与据点进入条件仍然有效。
        public override FloatMenuAcceptanceReport StillValid(Caravan caravan, PlanetTile destinationTile)
        {
            FloatMenuAcceptanceReport report = base.StillValid(caravan, destinationTile);
            if (!report) return report;
            if (site == null || site.Tile != destinationTile) return false;
            return CanEnter(caravan, site);
        }

        //抵达后通过原版长事件生成地图，避免在世界界面重入地图初始化。
        public override void Arrived(Caravan caravan)
        {
            FloatMenuAcceptanceReport report = CanEnter(caravan, site);
            if (!report)
            {
                Messages.Message(report.FailMessage, MessageTypeDefOf.RejectInput, false);
                return;
            }
            if (!site.HasMap)
                LongEventHandler.QueueLongEvent(() => Enter(caravan), "GeneratingMapForNewEncounter", false, null);
            else Enter(caravan);
        }

        //保持队伍物资从地图边缘部署，完成全部人员入场后才初始化挑战。
        private void Enter(Caravan caravan)
        {
            bool hadMap = site.HasMap;
            Map map = GetOrGenerateMapUtility.GetOrGenerateMap(site.Tile, HiveLordChallengeSite.ChallengeMapSize, null);
            if (!hadMap) Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
            CaravanEnterMapUtility.Enter(caravan, map, CaravanEnterMode.Edge, CaravanDropInventoryMode.DoNotDrop, draftColonists: false);
            site.NotifyEntered();
        }

        //拒绝已移除或等待清图的据点，并要求远行队携带能继续作战的殖民者。
        public static FloatMenuAcceptanceReport CanEnter(Caravan caravan, HiveLordChallengeSite site)
        {
            if (site == null || !site.Spawned) return false;
            if (site.Phase == HiveLordChallengePhase.WaitingForRetry && site.HasMap)
                return FloatMenuAcceptanceReport.WithFailMessage("HiveLord_Site_WaitForCleanup".Translate().ToString());
            if (site.EnterCooldownBlocksEntering())
                return FloatMenuAcceptanceReport.WithFailMessage("HiveLord_Site_EntryCooldown".Translate().ToString());
            foreach (Pawn pawn in caravan.PawnsListForReading)
                if (HiveLordChallengeParticipants.IsActive(pawn)) return true;
            return FloatMenuAcceptanceReport.WithFailMessage("HiveLord_Site_NeedFighter".Translate().ToString());
        }

        //生成原版远行队寻路菜单选项。
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, HiveLordChallengeSite site)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions(() => CanEnter(caravan, site),
                () => new CaravanArrivalAction_EnterHiveLordSite(site), "HiveLord_Site_Enter".Translate().ToString(), caravan, site.Tile, site);
        }

        //保存旅途中到达动作的目标据点引用。
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref site, "challengeSite");
        }
    }
}
