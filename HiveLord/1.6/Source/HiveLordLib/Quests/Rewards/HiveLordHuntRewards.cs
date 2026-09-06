using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HiveLordLib
{
    //将固定辛迪加通关奖励按原版堆叠上限装入空投，投送至挑战队伍附近。
    internal static class HiveLordHuntRewards
    {
        //按当前语言说明猎杀报酬。
        internal static string Description => "HiveLord_Quest_Rewards".Translate().ToString();

        //创建并投放一份完整奖励，空投物品允许玩家立即搬运。
        internal static void Deliver(Map map)
        {
            var rewards = new List<Thing>();
            AddStacks(rewards, ThingDefOf.Silver, 10000);
            AddStacks(rewards, DefDatabase<ThingDef>.GetNamed("Hyperweave"), 1000);
            AddStacks(rewards, HiveLordSummonDefOf.HiveLord_SummonTrainer, 1);
            Pawn receiver = HiveLordChallengeParticipants.FindEncounterTarget(map);
            IntVec3 center = receiver != null ? receiver.PositionHeld : map.Center;
            DropPodUtility.DropThingsNear(center, map, rewards, forbid: false, allowFogged: false);
        }

        //根据物品实际堆叠上限拆分数量，避免超大堆叠被生成流程截断。
        private static void AddStacks(List<Thing> things, ThingDef def, int count)
        {
            while (count > 0)
            {
                Thing thing = ThingMaker.MakeThing(def);
                thing.stackCount = System.Math.Min(count, def.stackLimit);
                count -= thing.stackCount;
                things.Add(thing);
            }
        }
    }
}
