using ServicePoint;
using TanksServer.GameLogic;
using TanksServer.Interactivity;

namespace TanksServer.Graphics;

internal sealed class GeneratePixelsTickStep(
    IEnumerable<IDrawStep> drawSteps,
    IEnumerable<IFrameConsumer> consumers
) : ITickStep, IDisposable
{
    private GamePixelGrid _lastGamePixelGrid = new(MapService.PixelsPerRow, MapService.PixelsPerColumn);
    private Bitmap _lastObserverPixelGrid = new(MapService.PixelsPerRow, MapService.PixelsPerColumn);
    private GamePixelGrid _gamePixelGrid = new(MapService.PixelsPerRow, MapService.PixelsPerColumn);
    private Bitmap _observerPixelGrid = new(MapService.PixelsPerRow, MapService.PixelsPerColumn);

    private readonly List<IDrawStep> _drawSteps = drawSteps.ToList();
    private readonly List<IFrameConsumer> _consumers = consumers.ToList();

    public async ValueTask TickAsync(TimeSpan _)
    {
        Draw(_gamePixelGrid, _observerPixelGrid);
        if (_observerPixelGrid.Equals(_lastObserverPixelGrid))
            return;

        await _consumers.Select(c => c.OnFrameDoneAsync(_gamePixelGrid, _observerPixelGrid))
            .WhenAll();

        (_lastGamePixelGrid, _gamePixelGrid) = (_gamePixelGrid, _lastGamePixelGrid);
        (_lastObserverPixelGrid, _observerPixelGrid) = (_observerPixelGrid, _lastObserverPixelGrid);
    }

    private void Draw(GamePixelGrid gamePixelGrid, Bitmap observerPixelGrid)
    {
        gamePixelGrid.Clear();
        foreach (var step in _drawSteps)
            step.Draw(gamePixelGrid);

        observerPixelGrid.Fill(false);
        for (ulong y = 0; y < MapService.PixelsPerColumn; y++)
            for (ulong x = 0; x < MapService.PixelsPerRow; x++)
            {
                if (gamePixelGrid[x, y].EntityType.HasValue)
                    observerPixelGrid.Set(x, y, true);
            }
    }

    public void Dispose()
    {
        _lastObserverPixelGrid.Dispose();
        _observerPixelGrid.Dispose();
    }
}
