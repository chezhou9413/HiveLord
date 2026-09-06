using System;
using Verse;

namespace HiveLordLib
{
    //提供霸王虫战斗与动画的开发者控制命令。
    public sealed partial class HiveLordProjectionThing
    {
        //创建用于开关简单战斗 AI 的开发者命令。
        private Command_Action CreateCombatAiCommand()
        {
            return new Command_Action
            {
                defaultLabel = CombatAiEnabled ? "HiveLord_Debug_DisableAI".Translate().ToString() : "HiveLord_Debug_EnableAI".Translate().ToString(),
                defaultDesc = "HiveLord_Debug_AIDesc".Translate().ToString(),
                icon = BaseContent.BadTex,
                action = () => SetCombatAiEnabled(!CombatAiEnabled)
            };
        }

        //创建清除强制目标和当前目标并立即重新搜索的开发者命令。
        private Command_Action CreateReacquireCommand()
        {
            Command_Action command = new Command_Action
            {
                defaultLabel = "HiveLord_Debug_Reacquire".Translate().ToString(),
                defaultDesc = "HiveLord_Debug_ReacquireDesc".Translate().ToString(),
                icon = BaseContent.BadTex,
                action = CombatController.ClearTargetAndReacquire
            };
            if (!CombatAiEnabled)
            {
                command.Disable("HiveLord_Debug_AIDisabled".Translate().ToString());
            }

            return command;
        }

        //创建用于开关自动展示循环的开发者命令。
        private Command_Action CreateAutomaticCommand()
        {
            return new Command_Action
            {
                defaultLabel = automaticCycle ? "HiveLord_Debug_StopCycle".Translate().ToString() : "HiveLord_Debug_StartCycle".Translate().ToString(),
                defaultDesc = "HiveLord_Debug_CycleDesc".Translate().ToString(),
                icon = BaseContent.BadTex,
                action = () => SetAutomaticCycle(!automaticCycle)
            };
        }

        //创建一个关闭 AI 并强制播放指定视觉状态的开发者命令。
        private Command_Action CreateStateCommand(HiveLordVisualState state)
        {
            return new Command_Action
            {
                defaultLabel = "HiveLord_Debug_StateLabel".Translate(GetStateLabel(state)),
                defaultDesc = "HiveLord_Debug_StateDesc".Translate().ToString(),
                icon = BaseContent.BadTex,
                action = () =>
                {
                    SetAutomaticCycle(false);
                    SetVisualState(state, true);
                }
            };
        }

        //把内部视觉状态转换为当前语言的开发者命令名称。
        private static string GetStateLabel(HiveLordVisualState state)
        {
            switch (state)
            {
                case HiveLordVisualState.Underground:
                    return "HiveLord_Debug_Underground".Translate().ToString();
                case HiveLordVisualState.Emerging:
                    return "HiveLord_Debug_Emerging".Translate().ToString();
                case HiveLordVisualState.Idle:
                    return "HiveLord_Debug_Idle".Translate().ToString();
                case HiveLordVisualState.AcidAttack:
                    return "HiveLord_Debug_Acid".Translate().ToString();
                case HiveLordVisualState.GroundSlam:
                    return "HiveLord_Debug_Slam".Translate().ToString();
                case HiveLordVisualState.Submerging:
                    return "HiveLord_Debug_Submerging".Translate().ToString();
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
