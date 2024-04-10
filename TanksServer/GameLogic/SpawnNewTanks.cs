namespace TanksServer.GameLogic;

internal sealed class SpawnNewTanks(TankManager tanks, MapService map, SpawnQueueProvider queueProvider) : ITickStep
{
    public Task TickAsync()
    {
        while (queueProvider.Queue.TryDequeue(out var player))
        {
            var tank = new Tank(player, ChooseSpawnPosition())
            {
                Rotation = Random.Shared.Next(0, 16)
            };
            tanks.Add(tank);
        }

        return Task.CompletedTask;
    }

    private FloatPosition ChooseSpawnPosition()
    {
        List<TilePosition> candidates = new();
        
        for (var x = 0; x < MapService.TilesPerRow; x++)
        for (var y = 0; y < MapService.TilesPerColumn; y++)
        {
            var tile = new TilePosition(x, y);

            if (map.IsCurrentlyWall(tile))
                continue;
            
            // TODO: check tanks and bullets
            candidates.Add(tile);
        }

        var chosenTile = candidates[Random.Shared.Next(candidates.Count)];
        return new FloatPosition(
            chosenTile.X * MapService.TileSize,
            chosenTile.Y * MapService.TileSize
        );
    }
}