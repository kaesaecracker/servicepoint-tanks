namespace TanksServer.GameLogic;

internal sealed class MoveTanks(
    MapEntityManager entityManager,
    IOptions<GameRules> options,
    MapService map
) : ITickStep
{
    private readonly GameRules _config = options.Value;

    public ValueTask TickAsync(TimeSpan delta)
    {
        foreach (var tank in entityManager.Tanks)
            tank.Moving = TryMoveTank(tank, delta);

        return ValueTask.CompletedTask;
    }

    private bool TryMoveTank(Tank tank, TimeSpan delta)
    {
        var player = tank.Owner;

        double speed;
        switch (player.Controls)
        {
            case { Forward: false, Backward: false }:
            case { Forward: true, Backward: true }:
                return false;
            case { Forward: true }:
                speed = +_config.MoveSpeed;
                break;
            case { Backward: true }:
                speed = -_config.MoveSpeed;
                break;
            default:
                return false;
        }

        speed *= delta.TotalSeconds;

        var angle = tank.Orientation / 16d * 2d * Math.PI;
        var newX = tank.Position.X + Math.Sin(angle) * speed;
        var newY = tank.Position.Y - Math.Cos(angle) * speed;

        return TryMoveTankTo(tank, new FloatPosition(newX, newY))
               || TryMoveTankTo(tank, new FloatPosition(newX, tank.Position.Y))
               || TryMoveTankTo(tank, new FloatPosition(tank.Position.X, newY));
    }

    private bool TryMoveTankTo(Tank tank, FloatPosition newPosition)
    {
        if (HitsWall(newPosition))
            return false;
        if (HitsTank(tank, newPosition))
            return false;

        tank.Owner.Scores.DistanceMoved += newPosition.Distance(tank.Position);
        tank.Position = newPosition;
        return true;
    }

    private bool HitsTank(Tank tank, FloatPosition newPosition) =>
        entityManager.Tanks
            .Where(otherTank => otherTank != tank)
            .Any(otherTank => newPosition.Distance(otherTank.Position) < MapService.TileSize);

    private bool HitsWall(FloatPosition newPosition)
    {
        var (topLeft, _) = newPosition.GetBoundsForCenter(MapService.TileSize);

        for (ulong y = 0; y < MapService.TileSize; y++)
            for (ulong x = 0; x < MapService.TileSize; x++)
            {
                var pixelToCheck = topLeft.GetPixelRelative((long)x, (long)y);
                if (map.Current.IsWall(pixelToCheck))
                    return true;
            }

        return false;
    }
}
