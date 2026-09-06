using System;
using LudeonTK;
using RimWorld;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //提供检查完整占地的生成接口与开发者地图放置入口。
    public static class SporeSpewerSpawnUtility
    {
        //按固定建筑的地形和占地规则验证位置，合法时生成虫族孢子喷涌虫。
        public static SporeSpewerThing Spawn(Map map, IntVec3 position)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            AcceptanceReport report = CanSpawnAt(map, position);
            if (!report.Accepted) throw new InvalidOperationException(report.Reason);
            var source = (SporeSpewerThing)ThingMaker.MakeThing(SporeSpewerDefOf.HiveLord_SporeSpewer);
            source.SetFaction(Faction.OfInsects);
            return (SporeSpewerThing)GenSpawn.Spawn(source, position, map, Rot4.North, WipeMode.VanishOrMoveAside);
        }

        //检查完整建筑占地及地形承载，允许原版生成流程清理植被但禁止覆盖建筑。
        public static AcceptanceReport CanSpawnAt(Map map, IntVec3 position)
        {
            if (map == null) return "HiveLord_Spore_NeedMap".Translate().ToString();
            CellRect occupied = GenAdj.OccupiedRect(position, Rot4.North, SporeSpewerDefOf.HiveLord_SporeSpewer.size);
            foreach (IntVec3 cell in occupied)
            {
                if (!cell.InBounds(map))
                    return "HiveLord_Spore_OutOfBounds".Translate(cell.ToString()).ToString();
                foreach (Thing thing in cell.GetThingList(map))
                    if (thing is Building)
                        return "HiveLord_Spore_Occupied".Translate(thing.LabelCap, cell.ToString()).ToString();
            }
            //扎根建筑使用地形承载能力，不要求占地中的每格都能供单位站立。
            if (!GenConstruct.CanBuildOnTerrain(SporeSpewerDefOf.HiveLord_SporeSpewer, position, map, Rot4.North))
                return "HiveLord_Spore_UnsupportedTerrain".Translate(position.ToString()).ToString();
            return AcceptanceReport.WasAccepted;
        }

        //使用当前语言构造开发者地图放置指令。
        [DebugAction("HiveLord", "Spawn Spore Spewer", actionType = DebugActionType.ToolMap,
            allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static DebugActionNode DebugSpawnAction()
        {
            return new DebugActionNode("HiveLord_Debug_SpawnSpore".Translate().ToString(), DebugActionType.ToolMap, DebugSpawn);
        }

        //检查点击位置并生成孢子喷涌虫，无效位置显示具体原因。
        private static void DebugSpawn()
        {
            Map map = Find.CurrentMap;
            IntVec3 position = UI.MouseCell();
            AcceptanceReport report = CanSpawnAt(map, position);
            if (!report.Accepted)
            {
                Messages.Message(report.Reason, MessageTypeDefOf.RejectInput, false);
                return;
            }
            Spawn(map, position);
        }
    }
}
