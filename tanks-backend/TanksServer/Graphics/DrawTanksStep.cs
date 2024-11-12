using TanksServer.GameLogic;

namespace TanksServer.Graphics;

internal sealed class DrawTanksStep(MapEntityManager entityManager) : IDrawStep
{
    private readonly SpriteSheet _tankSprites =
        SpriteSheet.FromImageFile("assets/tank.png", (int)MapService.TileSize, (int)MapService.TileSize);

    public void Draw(GamePixelGrid pixels)
    {
        foreach (var tank in entityManager.Tanks)
        {
            var tankPosition = tank.Bounds.TopLeft;

            for (byte dy = 0; dy < MapService.TileSize; dy++)
                for (byte dx = 0; dx < MapService.TileSize; dx++)
                {
                    var pixel = _tankSprites[tank.Orientation][dx, dy];
                    if (!pixel.HasValue || !pixel.Value)
                        continue;

                    var (x, y) = tankPosition.GetPixelRelative(dx, dy);
                    pixels[x, y].EntityType = GamePixelEntityType.Tank;
                    pixels[x, y].BelongsTo = tank.Owner;
                }
        }
    }
}
