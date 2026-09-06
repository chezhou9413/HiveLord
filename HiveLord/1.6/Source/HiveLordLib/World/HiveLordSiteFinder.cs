using RimWorld;
using RimWorld.Planet;
using Verse;

namespace HiveLordLib
{
    //优先寻找天然干旱平原，缺少时选取可达空地供接受委托后改造。
    internal static class HiveLordSiteFinder
    {
        internal const int MinTravelDistance = 8;
        internal const int MaxTravelDistance = 36;

        //分两轮搜索天然场地和可改造场地，只遍历远行队能够通行的路线。
        internal static bool TryFind(out PlanetTile tile)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                bool naturalOnly = pass == 0;
                foreach (Map home in Find.Maps)
                {
                    if (!IsSurfaceHome(home)) continue;
                    if (TileFinder.TryFindPassableTileWithTraversalDistance(home.Tile,
                        MinTravelDistance, MaxTravelDistance, out tile,
                        candidate => IsCandidate(candidate) && (!naturalOnly || IsNaturalPlain(candidate)),
                        tileFinderMode: TileFinderMode.Near, canTraverseImpassable: false)) return true;
                }
            }
            tile = PlanetTile.Invalid;
            return false;
        }

        //接受前重新验证场地占用与实际旅行距离，防止殖民地迁移后沿用不可达坐标。
        internal static bool IsAvailable(PlanetTile tile)
        {
            if (!IsCandidate(tile)) return false;
            foreach (Map home in Find.Maps)
            {
                if (!IsSurfaceHome(home) || home.Tile.Layer != tile.Layer) continue;
                int distance = Find.WorldGrid.TraversalDistanceBetween(home.Tile, tile,
                    passImpassable: false, maxDist: MaxTravelDistance);
                if (distance >= MinTravelDistance && distance <= MaxTravelDistance) return true;
            }
            return false;
        }

        //排除水面、不可通行格子和已有世界对象，保持殖民地与其他任务场地完整。
        internal static bool IsCandidate(PlanetTile tile)
        {
            return tile.Valid && Find.WorldGrid.InBounds(tile) && tile.Layer.IsRootSurface
                && tile.Tile is SurfaceTile && !tile.Tile.WaterCovered && !Find.World.Impassable(tile)
                && !Find.WorldObjects.AnyWorldObjectAt(tile)
                && TileFinder.IsValidTileForNewSettlement(tile);
        }

        //天然场地不得带有会改变地貌或生成环境的地标与地形变体。
        internal static bool IsNaturalPlain(PlanetTile tile)
        {
            return tile.Tile.PrimaryBiome.defName == "AridShrubland"
                && tile.Tile.hilliness == Hilliness.Flat
                && tile.Tile.Mutators.Count == 0 && tile.Tile.Landmark == null;
        }

        //限定从可供远行队出发的玩家地表殖民地进行搜索。
        private static bool IsSurfaceHome(Map home)
        {
            return home.IsPlayerHome && home.Tile.Valid && home.Tile.Layer.IsRootSurface
                && !Find.World.Impassable(home.Tile);
        }
    }
}
