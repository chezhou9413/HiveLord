using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace HiveLordLib
{
    //管理地表阶段随霸王虫出现的巨大受击建筑，并在入土后保存其生命值。
    public sealed partial class HiveLordProjectionThing
    {
        private HiveLordHitProxy hitProxy;
        private int storedHitPoints;
        private int storedMaxHitPoints;

        public int CurrentHealth => isDying ? 0 : hitProxy != null && !hitProxy.Destroyed
            ? hitProxy.HitPoints : storedHitPoints;
        public int MaximumHealth => storedMaxHitPoints > 0 ? storedMaxHitPoints : ConfiguredMaximumHealth;

        //保存受击代理引用和跨入土阶段保留的生命值。
        private void ExposeHitProxyData()
        {
            Scribe_References.Look(ref hitProxy, "hiveLordHitProxy");
            Scribe_Values.Look(ref storedHitPoints, "hiveLordStoredHitPoints", 0);
            Scribe_Values.Look(ref storedMaxHitPoints, "hiveLordStoredMaxHitPoints", 0);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && hitProxy != null)
            {
                hitProxy.BindOwner(this);
            }
        }

        //按当前视觉状态决定创建、跟随或收回受击代理。
        private void SynchronizeHitProxy()
        {
            if (!Spawned)
            {
                return;
            }

            SynchronizeHitProxyForState(visualState);
            if (hitProxy == null || hitProxy.Destroyed || !hitProxy.Spawned)
            {
                return;
            }

            storedHitPoints = hitProxy.HitPoints;
            if (hitProxy.Position != Position)
            {
                hitProxy.DeSpawn();
                GenSpawn.Spawn(hitProxy, Position, Map, Rot4.North, WipeMode.Vanish);
            }
        }

        //在虫体露出地面期间维持代理，完全入土后移除代理但保留生命值。
        private void SynchronizeHitProxyForState(HiveLordVisualState state)
        {
            if (!Spawned)
            {
                return;
            }

            if (isDying || state == HiveLordVisualState.Underground)
            {
                RetireHitProxy();
                return;
            }

            EnsureHitProxySpawned();
        }

        //创建与霸王虫同阵营的巨大无贴图建筑，并恢复上次入土前的生命值。
        private void EnsureHitProxySpawned()
        {
            SynchronizeDifficultyHealth();
            if (hitProxy != null && !hitProxy.Destroyed)
            {
                hitProxy.BindOwner(this);
                return;
            }

            HiveLordHitProxy proxy = ThingMaker.MakeThing(HitProxyDef)
                as HiveLordHitProxy;
            if (proxy == null)
            {
                throw new InvalidOperationException("HiveLord_Error_HitProxyClass".Translate().ToString());
            }

            proxy.BindOwner(this);
            proxy.SetFaction(Faction);
            GenSpawn.Spawn(proxy, Position, Map, Rot4.North, WipeMode.Vanish);
            proxy.HitPoints = storedHitPoints;
            storedHitPoints = proxy.HitPoints;
            hitProxy = proxy;
        }

        //无掉落地收回地表代理，并把最后生命值留给下一次出土。
        private void RetireHitProxy()
        {
            if (hitProxy == null)
            {
                return;
            }

            if (!hitProxy.Destroyed)
            {
                storedHitPoints = hitProxy.HitPoints;
                hitProxy.Destroy(DestroyMode.Vanish);
            }

            hitProxy = null;
        }

        //在受击代理生命值耗尽时移交死亡流程，保留用于尸体下沉的远端投影。
        internal void NotifyHitProxyKilled(HiveLordHitProxy killedProxy)
        {
            if (hitProxy != killedProxy || Destroyed)
            {
                return;
            }

            storedHitPoints = 0;
            hitProxy = null;
            BeginDeath();
        }

        //在地下及地表统一应用生命倍率，并保持已有血量比例。
        private void SynchronizeDifficultyHealth()
        {
            if (isDying) return;
            int maximum = ConfiguredMaximumHealth;
            bool hasProxy = hitProxy != null && !hitProxy.Destroyed;
            int health = hasProxy ? hitProxy.HitPoints : storedHitPoints;
            if (storedMaxHitPoints == 0)
            {
                health = maximum;
            }
            else if (storedMaxHitPoints != maximum)
            {
                health = Mathf.Clamp(Mathf.RoundToInt(health * (maximum / (float)storedMaxHitPoints)), 1, maximum);
            }
            storedMaxHitPoints = maximum;
            storedHitPoints = health;
            if (hasProxy) hitProxy.HitPoints = health;
        }
    }
}
