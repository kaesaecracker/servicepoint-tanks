using Microsoft.Extensions.Hosting;

internal class LoggingLifecycleService(ILogger logger) : IHostedLifecycleService
{
    private protected readonly ILogger Logger = logger;

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("StartAsync");
        return Task.CompletedTask;
    }

    public virtual Task StartedAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("StartedAsync");
        return Task.CompletedTask;
    }

    public virtual Task StartingAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("StartingAsync");
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("StopAsync");
        return Task.CompletedTask;
    }

    public virtual Task StoppedAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("StoppedAsync");
        return Task.CompletedTask;
    }

    public virtual Task StoppingAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("StoppingAsync");
        return Task.CompletedTask;
    }
}
