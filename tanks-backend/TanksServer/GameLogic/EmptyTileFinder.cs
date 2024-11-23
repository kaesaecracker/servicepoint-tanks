namespace TanksServer.GameLogic;

internal sealed class EmptyTileFinder(
    MapEntityManager entityManager,
    MapService mapService
)
{
    public TilePosition ChooseEmptyTile()
    {
        var maxMinDistance = 0d;
        TilePosition spawnTile = default;
        for (ushort x = 1; x < MapService.TilesPerRow - 1; x++)
            for (ushort y = 1; y < MapService.TilesPerColumn - 1; y++)
            {
                var tile = new TilePosition(x, y);
                if (mapService.Current.IsWall(tile))
                    continue;

                var tilePixelCenter = tile.GetCenter().ToFloatPosition();
                var minDistance = entityManager.AllEntities
                    .Select(entity => entity.Position.Distance(tilePixelCenter))
                    .Aggregate(double.MaxValue, Math.Min);
                if (minDistance <= maxMinDistance)
                    continue;

                maxMinDistance = minDistance;
                spawnTile = tile;
            }

        return spawnTile;
    }
}
