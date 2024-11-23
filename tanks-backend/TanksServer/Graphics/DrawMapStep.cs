using TanksServer.GameLogic;

namespace TanksServer.Graphics;

internal sealed class DrawMapStep(MapService map) : IDrawStep
{
    public void Draw(GamePixelGrid pixels) => Draw(pixels, map.Current);

    private static void Draw(GamePixelGrid pixels, Map map)
    {
        for (ulong y = 0; y < MapService.PixelsPerColumn; y++)
            for (ulong x = 0; x < MapService.PixelsPerRow; x++)
            {
                if (!map.IsWall(x, y))
                    continue;

                pixels[x, y].EntityType = GamePixelEntityType.Wall;
            }
    }

    public static void Draw(Bitmap pixels, Map map)
    {
        for (ulong y = 0; y < MapService.PixelsPerColumn; y++)
            for (ulong x = 0; x < MapService.PixelsPerRow; x++)
            {
                if (!map.IsWall(x, y))
                    continue;
                pixels.Set(x, y, true);
            }
    }
}
