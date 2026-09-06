using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace HiveLordLib
{
    //在接受委托时将最终候选空地改造成干旱平原，并同步世界缓存。
    internal static class HiveLordSiteTerrain
    {
        //保留天然场地，否则只改造经过可达性复核的目标格子。
        internal static void Prepare(PlanetTile tile)
        {
            if (!HiveLordSiteFinder.IsAvailable(tile))
                throw new InvalidOperationException("HiveLord_Quest_NoSite".Translate().ToString());
            if (HiveLordSiteFinder.IsNaturalPlain(tile)) return;

            SurfaceTile terrain = (SurfaceTile)tile.Tile;
            terrain.PrimaryBiome = DefDatabase<BiomeDef>.GetNamed("AridShrubland");
            terrain.hilliness = Hilliness.Flat;
            terrain.temperature = 26f;
            terrain.rainfall = 800f;
            terrain.swampiness = 0f;
            //清除该格的特殊地貌生成规则，避免平原仍生成峡谷、山体或混合生态。
            terrain.mutatorsNullable?.Clear();
            Find.World.landmarks.RemoveLandmark(tile);
            ResetTileCaches(terrain);
            Find.World.tileTemperatures.ClearCaches();
            Find.WorldPathGrid.RecalculatePerceivedMovementDifficultyAt(tile, out bool needsRecache);
            if (needsRecache) Find.WorldReachability.ClearCache();
            tile.Layer.FastTileFinder.DirtyCache();
            tile.Layer.SetAllLayersDirty();

            if (!HiveLordSiteFinder.IsAvailable(tile))
                throw new InvalidOperationException("HiveLord_Quest_NoSite".Translate().ToString());
        }

        //使原版惰性缓存按新生态、地貌和温度重新计算，避免显示旧山地或旧气候。
        private static void ResetTileCaches(Tile terrain)
        {
            AccessTools.Field(typeof(Tile), "hillinessLabelCached").SetValue(terrain, null);
            AccessTools.Field(typeof(Tile), "tmpHasSecondaryBiome").SetValue(terrain, null);
            AccessTools.Field(typeof(Tile), "tmpSecondaryBiome").SetValue(terrain, null);
            AccessTools.Field(typeof(Tile), "cachedMaxTemp").SetValue(terrain, null);
            AccessTools.Field(typeof(Tile), "cachedMinTemp").SetValue(terrain, null);
        }
    }
}
