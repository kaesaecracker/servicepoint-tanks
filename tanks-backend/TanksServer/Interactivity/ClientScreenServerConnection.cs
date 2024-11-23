using System.Buffers;
using System.Net.WebSockets;
using TanksServer.Graphics;

namespace TanksServer.Interactivity;

internal sealed class ClientScreenServerConnection
    : DroppablePackageRequestConnection<ClientScreenServerConnection.Package>
{
    private readonly BufferPool _bufferPool;
    private readonly PlayerScreenData? _playerDataBuilder;
    private readonly Player? _player;

    public ClientScreenServerConnection(
        WebSocket webSocket,
        ILogger<ClientScreenServerConnection> logger,
        Player? player,
        BufferPool bufferPool
    ) : base(logger, new ByteChannelWebSocket(webSocket, logger, 0))
    {
        _player = player;
        _bufferPool = bufferPool;
        _player?.IncrementConnectionCount();
        _playerDataBuilder = player == null
            ? null
            : new PlayerScreenData(logger, player);
    }

    public async Task OnGameTickAsync(Bitmap pixels, GamePixelGrid gamePixelGrid)
    {
        await Task.Yield();
        var next = BuildNextPackage(pixels, gamePixelGrid);
        SetNextPackage(next);
    }

    private Package BuildNextPackage(Bitmap pixels, GamePixelGrid gamePixelGrid)
    {
        var pixelsData = pixels.CopyRaw();
        var nextPixels = _bufferPool.Rent(pixelsData.Length);
        pixelsData.CopyTo(nextPixels.Memory.Span);

        if (_playerDataBuilder == null)
            return new Package(nextPixels, null);

        var data = _playerDataBuilder.Build(gamePixelGrid);
        var nextPlayerData = _bufferPool.Rent(data.Length);
        data.CopyTo(nextPlayerData.Memory);

        return new Package(nextPixels, nextPlayerData);
    }

    protected override async ValueTask SendPackageAsync(Package package)
    {
        try
        {
            await Socket.SendBinaryAsync(package.Pixels.Memory, package.PlayerData == null);
            if (package.PlayerData != null)
                await Socket.SendBinaryAsync(package.PlayerData.Memory);
        }
        catch (WebSocketException ex)
        {
            Logger.LogWarning(ex, "send failed");
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _player?.DecrementConnectionCount();
    }

    internal sealed record class Package(
        IMemoryOwner<byte> Pixels,
        IMemoryOwner<byte>? PlayerData
    ) : IDisposable
    {
        public void Dispose()
        {
            Pixels.Dispose();
            PlayerData?.Dispose();
        }
    }
}
