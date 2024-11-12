using System.Diagnostics;

namespace TanksServer.Interactivity;

internal abstract class WebsocketServer<T>(
    ILogger logger
) : LoggingLifecycleService(logger)
    where T : WebsocketServerConnection
{
    private bool _closing;
    private readonly ConcurrentDictionary<T, byte> _connections = [];

    public async override Task StoppingAsync(CancellationToken cancellationToken)
    {
        await base.StoppingAsync(cancellationToken);
        _closing = true;
        Logger.LogInformation("closing connections");
        await _connections.Keys.Select(c => c.CloseAsync())
            .WhenAll();
        Logger.LogInformation("closed connections");
    }

    protected IEnumerable<T> Connections => _connections.Keys;

    protected async Task HandleClientAsync(T connection)
    {
        if (_closing)
        {
            Logger.LogWarning("refusing connection because server is shutting down");
            await connection.CloseAsync();
            return;
        }

        var added = _connections.TryAdd(connection, 0);
        Debug.Assert(added);

        await connection.ReceiveAsync();

        _ = _connections.TryRemove(connection, out _);
        connection.Dispose();
    }
}
