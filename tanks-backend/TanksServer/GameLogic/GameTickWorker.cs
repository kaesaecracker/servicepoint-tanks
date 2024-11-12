using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace TanksServer.GameLogic;

internal sealed class GameTickWorker(
    IEnumerable<ITickStep> steps,
    IHostApplicationLifetime lifetime,
    ILogger<GameTickWorker> logger
) : BackgroundService, IDisposable
{
    private readonly List<ITickStep> _steps = steps.ToList();

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        // the first tick is really short (< 0.01ms) if this line is directly above the while
        var sw = Stopwatch.StartNew();
        await Task.Delay(1, CancellationToken.None).ConfigureAwait(false);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var delta = sw.Elapsed;
                sw.Restart();

                foreach (var step in _steps)
                    await step.TickAsync(delta);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "game tick service crashed");
            lifetime.StopApplication();
        }
    }
}
