using System.Diagnostics;
using TanksServer.GameLogic;

namespace TanksServer.Models;

[DebuggerDisplay("({X} | {Y})")]
internal readonly struct PixelPosition(ulong x, ulong y)
{
    public ulong X { get; } = (x + MapService.PixelsPerRow) % MapService.PixelsPerRow;
    public ulong Y { get; } = (y + MapService.PixelsPerColumn) % MapService.PixelsPerColumn;

    public void Deconstruct(out ulong x, out ulong y)
    {
        x = X;
        y = Y;
    }
}
