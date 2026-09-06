using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HiveLordLib.SporeSpewer
{
    //管理本地图孢子源、非叠加减益、雾浓度及共享虫体捕获资源。
    public sealed class SporeSpewerMapComponent : MapComponent, System.IDisposable
    {
        private static SporeSpewerMapComponent activeCapture;
        private readonly HashSet<SporeSpewerThing> sources = new HashSet<SporeSpewerThing>();
        private List<SporeCollapseState> collapses = new List<SporeCollapseState>();
        private float fogDensity;
        private SporeFogRenderer fogRenderer;
        private SporeSpewerCapture capture;
        public int ActiveCount => sources.Count;
        public bool IsActive => ActiveCount > 0;
        public float FogDensity => fogDensity;

        //绑定地图，延迟创建仅可见地图需要的渲染资源。
        public SporeSpewerMapComponent(Map map) : base(map) { }

        //登记存活孢子源，首只出现时立即同步全图单位。
        internal void Register(SporeSpewerThing source)
        {
            if (source.Spawned && !source.Destroyed && source.HitPoints > 0
                && sources.Add(source) && sources.Count == 1)
                SynchronizePawns();
        }

        //注销来源，最后一只移除时立即解除单位减益。
        internal void Unregister(SporeSpewerThing source)
        {
            if (sources.Remove(source) && sources.Count == 0)
            {
                SynchronizePawns();
                if (collapses.Count == 0) ReleaseCapture();
            }
        }

        //在建筑移除前留下倒塌记录，使视觉残留与已结束的孢子影响分离。
        internal void BeginCollapse(SporeSpewerThing source)
        {
            collapses.Add(new SporeCollapseState(source.DrawPos));
            Unregister(source);
            SporeVisualEffects.Burst(map, source.DrawPos + SporeSpewerView.SporeOutletOffset);
        }

        //读档完成后从地图实体重建孢子源，避免序列化重复登记。
        public override void FinalizeInit()
        {
            sources.Clear();
            foreach (Thing thing in map.listerThings.ThingsOfDef(SporeSpewerDefOf.HiveLord_SporeSpewer))
                if (!thing.Destroyed && thing.HitPoints > 0)
                    sources.Add((SporeSpewerThing)thing);
            SynchronizePawns();
        }

        //保存雾过渡与倒塌进度，活动源数量由实际地图实体重建。
        public override void ExposeData()
        {
            Scribe_Values.Look(ref fogDensity, "sporeFogDensity");
            Scribe_Collections.Look(ref collapses, "sporeCollapses", LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && collapses == null)
                collapses = new List<SporeCollapseState>();
        }

        //按游戏时间推进雾浓度，每秒同步单位并生成低密度漂浮孢子。
        public override void MapComponentTick()
        {
            fogDensity = Mathf.MoveTowards(fogDensity, IsActive ? 1f : 0f, IsActive ? 1f / 180f : 1f / 300f);
            int ticks = Find.TickManager.TicksGame;
            for (int i = collapses.Count - 1; i >= 0; i--)
            {
                SporeCollapseState collapse = collapses[i];
                collapse.Tick(map);
                if (!collapse.Finished) continue;
                capture?.ReleaseCollapse(collapse);
                collapses.RemoveAt(i);
            }
            if (!IsActive && collapses.Count == 0) ReleaseCapture();
            if (ticks % 60 == 0) SynchronizePawns();
            if (map == Find.CurrentMap && fogDensity > 0f && ticks % 20 == 0)
                SporeVisualEffects.EmitMapSpores(map, fogDensity);
        }

        //仅绘制当前地图的橙雾，并释放切换到后台地图的摄像机资源。
        public override void MapComponentUpdate()
        {
            if (map != Find.CurrentMap || RimWorld.Planet.WorldRendererUtility.WorldSelected)
            {
                ReleaseCapture();
                return;
            }
            if (fogDensity > 0f)
            {
                if (fogRenderer == null) fogRenderer = new SporeFogRenderer();
                fogRenderer.Draw(map, fogDensity);
            }
            if (!IsActive && collapses.Count == 0) return;
            if (capture == null)
            {
                //未显示的地图不保证继续逐帧更新，因此在新地图捕获前主动释放旧舞台。
                if (activeCapture != null && activeCapture != this) activeCapture.ReleaseCapture();
                capture = new SporeSpewerCapture();
                activeCapture = this;
            }
            if (IsActive) capture.UpdateCapture();
            foreach (SporeSpewerThing source in sources) capture.Draw(source);
            foreach (SporeCollapseState collapse in collapses) capture.DrawCollapse(collapse);
        }

        //地图移除时解除健康状态并释放本地图持有的 Unity 资源。
        public override void MapRemoved()
        {
            sources.Clear();
            collapses.Clear();
            SynchronizePawns();
            Dispose();
        }

        //在卸载游戏或移除地图时释放全部地图专属渲染资源。
        public void Dispose()
        {
            ReleaseCapture();
            fogRenderer?.Dispose();
            fogRenderer = null;
        }

        //对所有已生成单位同步一次无阵营和种族豁免的地图减益。
        private void SynchronizePawns()
        {
            foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                SporeDebuffUtility.Synchronize(pawn, IsActive && !pawn.Dead);
        }

        //销毁本地图共享的虫体捕获舞台。
        private void ReleaseCapture()
        {
            capture?.Dispose();
            capture = null;
            if (activeCapture == this) activeCapture = null;
        }
    }
}
