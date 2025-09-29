using TanksServer.GameLogic;

namespace TanksServer.Graphics;

internal sealed class DrawPowerUpsStep(MapEntityManager entityManager) : IDrawStep
{
    private readonly Sprite _genericSprite = Sprite.FromImageFile(Path.Combine(Program.AssetsDir, "powerup_generic.png"));
    private readonly Sprite _smartSprite = Sprite.FromImageFile(Path.Combine(Program.AssetsDir, "powerup_smart.png"));
    private readonly Sprite _magazineSprite = Sprite.FromImageFile(Path.Combine(Program.AssetsDir, "powerup_magazine.png"));
    private readonly Sprite _explosiveSprite = Sprite.FromImageFile(Path.Combine(Program.AssetsDir, "powerup_explosive.png"));
    private readonly Sprite _fastSprite = Sprite.FromImageFile(Path.Combine(Program.AssetsDir, "powerup_fastbullet.png"));

    public void Draw(GamePixelGrid pixels)
    {
        foreach (var powerUp in entityManager.PowerUps)
        {
            var sprite = powerUp.Type switch
            {
                PowerUpType.MagazineSize => _magazineSprite,
                PowerUpType.BulletAcceleration or PowerUpType.BulletSpeed => _fastSprite,
                PowerUpType.SmartBullets => _smartSprite,
                PowerUpType.ExplosiveBullets => _explosiveSprite,
                _ => _genericSprite
            };

            DrawPowerUp(pixels, sprite, powerUp.Bounds.TopLeft);
        }
    }

    private static void DrawPowerUp(GamePixelGrid pixels, Sprite sprite, PixelPosition position)
    {
        for (byte dy = 0; dy < MapService.TileSize; dy++)
            for (byte dx = 0; dx < MapService.TileSize; dx++)
            {
                var pixelState = sprite[dx, dy];
                if (!pixelState.HasValue)
                    continue;

                var (x, y) = position.GetPixelRelative(dx, dy);
                pixels[x, y].EntityType = pixelState.Value
                    ? GamePixelEntityType.PowerUp
                    : null;
            }
    }
}
