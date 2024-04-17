using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TanksServer.GameLogic;

namespace TanksServer.Graphics;

internal sealed class DrawTanksStep : IDrawStep
{
    private readonly MapEntityManager _entityManager;
    private readonly bool[] _tankSprite;
    private readonly int _tankSpriteWidth;

    public DrawTanksStep(MapEntityManager entityManager)
    {
        _entityManager = entityManager;

        using var tankImage = Image.Load<Rgba32>("assets/tank.png");
        _tankSprite = new bool[tankImage.Height * tankImage.Width];

        var whitePixel = new Rgba32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        var i = 0;
        for (var y = 0; y < tankImage.Height; y++)
        for (var x = 0; x < tankImage.Width; x++, i++)
            _tankSprite[i] = tankImage[x, y] == whitePixel;

        _tankSpriteWidth = tankImage.Width;
    }

    public void Draw(GamePixelGrid pixels)
    {
        foreach (var tank in _entityManager.Tanks)
        {
            var tankPosition = tank.Bounds.TopLeft;

            for (byte dy = 0; dy < MapService.TileSize; dy++)
            for (byte dx = 0; dx < MapService.TileSize; dx++)
            {
                if (!TankSpriteAt(dx, dy, tank.Orientation))
                    continue;

                var (x, y) = tankPosition.GetPixelRelative(dx, dy);
                pixels[x, y].EntityType = GamePixelEntityType.Tank;
                pixels[x, y].BelongsTo = tank.Owner;
            }
        }
    }

    private bool TankSpriteAt(int dx, int dy, int tankRotation)
    {
        var x = tankRotation % 4 * (MapService.TileSize + 1);
        var y = (int)Math.Floor(tankRotation / 4d) * (MapService.TileSize + 1);
        var index = (y + dy) * _tankSpriteWidth + x + dx;

        return _tankSprite[index];
    }
}
