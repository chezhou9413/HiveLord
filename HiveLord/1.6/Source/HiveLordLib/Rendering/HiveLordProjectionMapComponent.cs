using System;
using System.Collections.Generic;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //管理地图中的任务霸王虫与召唤体，分别维护可见捕获舞台和生命周期。
    public sealed class HiveLordProjectionMapComponent : MapComponent, IDisposable
    {
        private static HiveLordProjectionMapComponent activeRenderer;
        private readonly HashSet<HiveLordProjectionThing> owners = new HashSet<HiveLordProjectionThing>();
        private readonly Dictionary<HiveLordProjectionThing, HiveLordProjectionStage> stages =
            new Dictionary<HiveLordProjectionThing, HiveLordProjectionStage>();
        private readonly HashSet<HiveLordProjectionThing> failedStages = new HashSet<HiveLordProjectionThing>();

        //绑定所属地图，捕获资源仅在虫体进入视野时创建。
        public HiveLordProjectionMapComponent(Map map) : base(map) { }

        //登记独立霸王虫实例，允许召唤体与任务目标同时存在。
        public bool Register(HiveLordProjectionThing thing)
        {
            if (thing == null) throw new ArgumentNullException(nameof(thing));
            owners.Add(thing);
            failedStages.Remove(thing);
            return true;
        }

        //移除离图实例并释放其专属捕获资源。
        public void Unregister(HiveLordProjectionThing thing)
        {
            owners.Remove(thing);
            failedStages.Remove(thing);
            ReleaseStage(thing);
        }

        //绘制当前地图所有可见实例，切换地图时释放上一地图的舞台。
        public override void MapComponentUpdate()
        {
            if (Find.CurrentMap != map || WorldRendererUtility.WorldSelected || Find.CameraDriver == null)
            {
                CleanupStages();
                return;
            }
            if (activeRenderer != null && activeRenderer != this) activeRenderer.CleanupStages();
            activeRenderer = this;
            foreach (HiveLordProjectionThing thing in owners)
            {
                if (!thing.Spawned || thing.Destroyed || !InView(thing))
                {
                    ReleaseStage(thing);
                    continue;
                }
                thing.DrawCombatDamageArea();
                HiveLordProjectionStage stage = EnsureStage(thing);
                if (stage == null) continue;
                stage.UpdateAndRender(thing);
                stage.Draw(thing);
            }
        }

        //保留任务霸王虫顶部血条，召唤体通过选中面板查看生命和剩余时间。
        public override void MapComponentOnGUI()
        {
            if (Event.current.type != EventType.Repaint || Find.CurrentMap != map || WorldRendererUtility.WorldSelected) return;
            foreach (HiveLordProjectionThing thing in owners)
            {
                if (!thing.Spawned || thing.IsDying || thing.IsSummoned) continue;
                HiveLordHudOverlay.Draw(thing);
                break;
            }
        }

        //移除地图时释放资源并清空实例登记。
        public override void MapRemoved()
        {
            Dispose();
            base.MapRemoved();
        }

        //卸载地图或切换存档时释放全部捕获资源。
        public void Dispose()
        {
            CleanupStages();
            owners.Clear();
            failedStages.Clear();
        }

        //按各实例显示比例裁剪地图视野外的投影。
        private bool InView(HiveLordProjectionThing thing)
        {
            int size = Mathf.CeilToInt(HiveLordProjectionStage.ProjectionSize * thing.SizeFactor) + 32;
            return new CellRect(thing.Position.x - size / 2, thing.Position.z - size / 2, size, size)
                .Overlaps(Find.CameraDriver.CurrentViewRect);
        }

        //为首次可见实例创建舞台，初始化异常只报告一次。
        private HiveLordProjectionStage EnsureStage(HiveLordProjectionThing thing)
        {
            if (stages.TryGetValue(thing, out HiveLordProjectionStage stage)) return stage;
            if (failedStages.Contains(thing)) return null;
            try
            {
                stage = new HiveLordProjectionStage();
                stages.Add(thing, stage);
                return stage;
            }
            catch (Exception exception)
            {
                failedStages.Add(thing);
                Log.Error("[HiveLord/Rendering] Capture stage initialization failed: " + exception);
                return null;
            }
        }

        //释放一个离图或不再可见的实例舞台。
        private void ReleaseStage(HiveLordProjectionThing thing)
        {
            if (!stages.TryGetValue(thing, out HiveLordProjectionStage stage)) return;
            stage.Dispose();
            stages.Remove(thing);
        }

        //集中清理地图捕获纹理、模型和摄像机。
        private void CleanupStages()
        {
            foreach (HiveLordProjectionStage stage in stages.Values) stage.Dispose();
            stages.Clear();
            if (activeRenderer == this) activeRenderer = null;
        }
    }
}
