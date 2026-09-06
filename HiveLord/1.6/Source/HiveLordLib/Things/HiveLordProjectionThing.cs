using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //作为霸王虫地图锚点，持有战斗存档、视觉状态、虫族阵营和公共控制接口。
    public sealed partial class HiveLordProjectionThing : Building
    {
        private HiveLordVisualState visualState = HiveLordVisualState.Underground;
        private bool automaticCycle;
        private bool restartRequested = true;
        private int stateRevision;
        private HiveLordCombatController combatController;
        private HiveLordCombatExtension combatSettings;

        public HiveLordVisualState VisualState => visualState;
        public bool AutomaticCycle => automaticCycle;
        public bool CombatAiEnabled => CombatController.Enabled;
        public Thing CombatTarget => CombatController.CombatTarget;
        internal bool RestartRequested => restartRequested;
        internal int StateRevision => stateRevision;
        internal IntVec3 CombatAimCell => CombatController.AimCell;
        internal float CombatVisualProgress => CombatController.GetVisualProgress();
        internal HiveLordCombatExtension CombatSettings => combatSettings ?? (combatSettings = CreateCombatSettings());
        private HiveLordCombatController CombatController => combatController
            ?? (combatController = new HiveLordCombatController(this));

        //按实例取得战斗参数，召唤体单独缩放范围而不修改共享定义。
        private HiveLordCombatExtension CreateCombatSettings()
        {
            HiveLordCombatExtension settings = def.GetModExtension<HiveLordCombatExtension>()
                ?? throw new InvalidOperationException("HiveLord_Error_CombatExtension".Translate().ToString());
            return IsSummoned ? settings.WithRangeScale(SizeFactor) : settings;
        }

        //让地图组件在每个渲染帧绘制当前攻击的伤害范围预警。
        internal void DrawCombatDamageArea()
        {
            CombatController.DrawDamageArea();
        }

        //生成到地图时固定为虫族阵营、恢复战斗阶段并登记唯一投影实例。
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            NormalizeFaction();
            SynchronizeDifficultyHealth();
            CombatController.PostSpawnSetup(respawningAfterLoad);
            map.GetComponent<HiveLordProjectionMapComponent>().Register(this);
            SynchronizeHitProxy();
        }

        //处理开发者生成器覆盖阵营的行为，确保生成后仍归属虫族。
        public override void Notify_DebugSpawned()
        {
            base.Notify_DebugSpawned();
            NormalizeFaction();
        }

        //每个正常 Thing Tick 推进战斗状态机，并在出入土动画期间持续生成碎石与烟尘。
        protected override void Tick()
        {
            base.Tick();
            if (TryExpireSummon()) return;
            if (isDying)
            {
                TickDeath();
                return;
            }
            SynchronizeDifficultyHealth();
            CombatController.Tick();
            SynchronizeHitProxy();
            if (visualState == HiveLordVisualState.Emerging)
            {
                HiveLordCombatEffects.TickEmergenceDebris(this);
            }
            else if (visualState == HiveLordVisualState.Submerging)
            {
                HiveLordCombatEffects.TickSubmergingDebris(this);
            }
        }

        //离开地图前注销投影实例并释放远端舞台。
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Map currentMap = Map;
            RetireHitProxy();
            if (currentMap != null)
            {
                currentMap.GetComponent<HiveLordProjectionMapComponent>().Unregister(this);
            }

            base.DeSpawn(mode);
        }

        //禁止 Thing 自身绘制占位贴图，实际画面由地图组件输出。
        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
        }

        //保存视觉开关和完整战斗进度，不序列化 Unity 运行时对象。
        public override void ExposeData()
        {
            base.ExposeData();
            ExposeSummonData();
            Scribe_Values.Look(ref visualState, "visualState", HiveLordVisualState.Underground);
            Scribe_Values.Look(ref automaticCycle, "automaticCycle", false);
            CombatController.ExposeData();
            ExposeHitProxyData();
            ExposeDeathData();

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (CombatController.Enabled)
                {
                    automaticCycle = false;
                }

                restartRequested = true;
                stateRevision++;
            }
        }

        //供外部手动播放视觉状态；手动接管时关闭战斗 AI。
        public void SetVisualState(HiveLordVisualState state, bool restart = true)
        {
            if (isDying) return;
            CombatController.SetEnabled(false);
            ApplyVisualState(state, restart);
        }

        //切换自动展示循环；启用展示时关闭战斗 AI。
        public void SetAutomaticCycle(bool enabled)
        {
            if (isDying) return;
            if (enabled)
            {
                CombatController.SetEnabled(false);
            }

            SetAutomaticCycleCore(enabled);
        }

        //公开切换简单战斗 AI，重新开启会从地下阶段重新开始。
        public void SetCombatAiEnabled(bool enabled)
        {
            CombatController.SetEnabled(enabled);
        }

        //公开设置下一轮强制目标，空值会清除强制目标并恢复自动搜索。
        public void SetForcedCombatTarget(Thing target)
        {
            if (isDying) return;
            CombatController.SetForcedTarget(target);
        }

        //允许战斗状态机切换动画而不触发手动接管逻辑。
        internal void SetVisualStateFromCombat(HiveLordVisualState state, bool restart)
        {
            ApplyVisualState(state, restart);
        }

        //允许战斗状态机关闭展示循环而不反向关闭自己。
        internal void SetAutomaticCycleFromCombat(bool enabled)
        {
            SetAutomaticCycleCore(enabled);
        }

        //在开发者模式提供战斗 AI、重新索敌、展示循环和手动动画按钮。
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }

            if (!Prefs.DevMode || isDying)
            {
                yield break;
            }

            yield return CreateCombatAiCommand();
            yield return CreateReacquireCommand();
            yield return CreateAutomaticCommand();
            foreach (HiveLordVisualState state in Enum.GetValues(typeof(HiveLordVisualState)))
            {
                yield return CreateStateCommand(state);
            }
        }

        //写入经过枚举验证的视觉状态并提高修订号供渲染端同步。
        private void ApplyVisualState(HiveLordVisualState state, bool restart)
        {
            if (isDying) return;
            if (!Enum.IsDefined(typeof(HiveLordVisualState), state))
            {
                throw new ArgumentOutOfRangeException(nameof(state), state, "HiveLord_Error_VisualState".Translate().ToString());
            }

            bool stateChanged = visualState != state;
            if (!stateChanged && !restart)
            {
                return;
            }

            visualState = state;
            restartRequested = restart || !stateChanged;
            stateRevision++;
            SynchronizeHitProxyForState(state);
            if (state == HiveLordVisualState.Emerging)
            {
                HiveLordCombatEffects.SpawnEmergenceBurst(this);
            }
            else if (state == HiveLordVisualState.Submerging)
            {
                HiveLordCombatEffects.SpawnSubmergingBurst(this);
            }
        }

        //写入展示循环开关并通知远端视觉控制器重新同步。
        private void SetAutomaticCycleCore(bool enabled)
        {
            if (automaticCycle == enabled)
            {
                return;
            }

            automaticCycle = enabled;
            restartRequested = true;
            stateRevision++;
        }

        //把开发者生成和读档中的旧阵营统一转换为虫族。
        private void NormalizeFaction()
        {
            if (!IsSummoned && Faction != RimWorld.Faction.OfInsects)
            {
                SetFaction(RimWorld.Faction.OfInsects);
            }
        }

    }
}
