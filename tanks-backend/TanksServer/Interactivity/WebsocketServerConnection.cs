namespace TanksServer.Interactivity;

internal abstract class WebsocketServerConnection(
    ILogger logger,
    ByteChannelWebSocket socket
) : IDisposable
{
    protected readonly ByteChannelWebSocket Socket = socket;
    protected readonly ILogger Logger = logger;

    public Task CloseAsync()
    {
        Logger.LogDebug("closing connection");
        return Socket.CloseAsync();
    }

    public async Task ReceiveAsync()
    {
        await foreach (var buffer in Socket.ReadAllAsync())
            await HandleMessageAsync(buffer);
        Logger.LogTrace("done receiving");
    }

    protected abstract ValueTask HandleMessageAsync(Memory<byte> buffer);

    public abstract void Dispose();
}
