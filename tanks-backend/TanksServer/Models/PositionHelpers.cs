using TanksServer.GameLogic;

namespace TanksServer.Models;

internal static class PositionHelpers
{
    public static PixelPosition GetPixelRelative(this PixelPosition position, long subX, long subY)
        => new((ulong)((long)position.X + subX), (ulong)((long)position.Y + subY));

    public static PixelPosition ToPixelPosition(this FloatPosition position)
        => new((ulong)Math.Round(position.X), (ulong)Math.Round(position.Y));

    public static PixelPosition ToPixelPosition(this TilePosition position) => new(
        (ulong)(position.X * MapService.TileSize),
        (ulong)(position.Y * MapService.TileSize)
    );

    public static TilePosition ToTilePosition(this PixelPosition position) => new(
        (ulong)(position.X / MapService.TileSize),
        (ulong)(position.Y / MapService.TileSize)
    );

    public static FloatPosition ToFloatPosition(this PixelPosition position) => new(position.X, position.Y);

    public static double Distance(this FloatPosition p1, FloatPosition p2)
        => Math.Sqrt(
            Math.Pow(p1.X - p2.X, 2) +
            Math.Pow(p1.Y - p2.Y, 2)
        );

    public static PixelBounds GetBoundsForCenter(this FloatPosition position, ulong size)
    {
        var sub = (long)(-(long)size / 2d);
        var add = (long)(size / 2d - 1);
        var pixelPosition = position.ToPixelPosition();
        return new PixelBounds(
            pixelPosition.GetPixelRelative(sub, sub),
            pixelPosition.GetPixelRelative(add, add)
        );
    }

    public static PixelPosition GetCenter(this TilePosition tile)
        => tile.ToPixelPosition().GetPixelRelative(4, 4);
}
