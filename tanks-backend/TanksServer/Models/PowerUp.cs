using TanksServer.GameLogic;

namespace TanksServer.Models;

internal enum PowerUpType
{
    MagazineSize,
    BulletSpeed,
    BulletAcceleration,
    ExplosiveBullets,
    SmartBullets,
}

internal sealed class PowerUp : IMapEntity
{
    public required FloatPosition Position { get; init; }

    public PixelBounds Bounds => Position.GetBoundsForCenter(MapService.TileSize);

    public required PowerUpType Type { get; init; }
}
