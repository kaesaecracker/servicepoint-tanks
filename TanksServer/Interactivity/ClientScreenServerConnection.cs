using System.Diagnostics;
using System.Net.WebSockets;
using DisplayCommands;
using TanksServer.Graphics;

namespace TanksServer.Interactivity;

internal sealed class ClientScreenServerConnection : IDisposable
{
    private readonly ByteChannelWebSocket _channel;
    private readonly ILogger<ClientScreenServerConnection> _logger;
    private readonly ClientScreenServer _server;
    private readonly SemaphoreSlim _wantedFrames = new(1);
    private readonly Guid? _playerGuid = null;
    private readonly PlayerScreenData? _playerScreenData = null;

    public ClientScreenServerConnection(
        WebSocket webSocket,
        ILogger<ClientScreenServerConnection> logger,
        ClientScreenServer server,
        Guid? playerGuid = null
    )
    {
        _server = server;
        _logger = logger;

        _playerGuid = playerGuid;
        if (playerGuid.HasValue)
            _playerScreenData = new PlayerScreenData(logger);

        _channel = new ByteChannelWebSocket(webSocket, logger, 0);
        Done = ReceiveAsync();
    }

    public Task Done { get; }

    public void Dispose()
    {
        _wantedFrames.Dispose();
        Done.Dispose();
    }

    public async Task SendAsync(PixelGrid pixels, GamePixelGrid gamePixelGrid)
    {
        if (!await _wantedFrames.WaitAsync(TimeSpan.Zero))
        {
            _logger.LogTrace("client does not want a frame yet");
            return;
        }

        if (_playerScreenData != null)
            RefreshPlayerSpecificData(gamePixelGrid);

        _logger.LogTrace("sending");
        try
        {
            _logger.LogTrace("sending {} bytes of pixel data", pixels.Data.Length);
            await _channel.SendAsync(pixels.Data, _playerScreenData == null);
            if (_playerScreenData != null)
                await _channel.SendAsync(_playerScreenData.GetPacket());
        }
        catch (WebSocketException ex)
        {
            _logger.LogWarning(ex, "send failed");
        }
    }

    private void RefreshPlayerSpecificData(GamePixelGrid gamePixelGrid)
    {
        Debug.Assert(_playerScreenData != null);
        _playerScreenData.Clear();
        foreach (var gamePixel in gamePixelGrid)
        {
            if (!gamePixel.EntityType.HasValue)
                continue;
            _playerScreenData.Add(gamePixel.EntityType.Value, gamePixel.BelongsTo?.Id == _playerGuid);
        }
    }

    private async Task ReceiveAsync()
    {
        await foreach (var _ in _channel.ReadAllAsync())
            _wantedFrames.Release();

        _logger.LogTrace("done receiving");
        _server.Remove(this);
    }

    public Task CloseAsync()
    {
        _logger.LogDebug("closing connection");
        return _channel.CloseAsync();
    }
}