using System.Diagnostics;
using TanksServer.GameLogic;

namespace TanksServer.Models;

[DebuggerDisplay("({X} | {Y})")]
internal readonly struct TilePosition(ulong x, ulong y)
{
    public ulong X { get; } = (ulong)((x + MapService.TilesPerRow) % MapService.TilesPerRow);
    public ulong Y { get; } = (ulong)((y + MapService.TilesPerColumn) % MapService.TilesPerColumn);
}
