using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using ServicePoint;
using TanksServer.GameLogic;
using TanksServer.Graphics;

namespace TanksServer.Interactivity;

internal sealed class SendToServicePointDisplay : IFrameConsumer, IDisposable
{
    private const int ScoresWidth = 12;
    private const int ScoresHeight = 20;
    private const int ScoresPlayerRows = ScoresHeight - 6;

    private readonly Connection _displayConnection;
    private readonly MapService _mapService;
    private readonly ILogger<SendToServicePointDisplay> _logger;
    private readonly PlayerServer _players;
    private readonly CharGrid _scoresBuffer;
    private readonly TimeSpan _minFrameTime;
    private readonly IOptionsMonitor<HostConfiguration> _options;

    private DateTime _nextFailLogAfter = DateTime.Now;
    private DateTime _nextFrameAfter = DateTime.Now;

    public SendToServicePointDisplay(
        PlayerServer players,
        ILogger<SendToServicePointDisplay> logger,
        Connection displayConnection,
        IOptions<HostConfiguration> hostOptions,
        MapService mapService,
        IOptionsMonitor<HostConfiguration> options,
        IOptions<DisplayConfiguration> displayConfig)
    {
        _players = players;
        _logger = logger;
        _displayConnection = displayConnection;
        _mapService = mapService;
        _minFrameTime = TimeSpan.FromMilliseconds(hostOptions.Value.ServicePointDisplayMinFrameTimeMs);
        _options = options;

        var localIp = GetLocalIPv4(displayConfig.Value).Split('.');
        Debug.Assert(localIp.Length == 4);
        _scoresBuffer = new CharGrid(12, 20);

        _scoresBuffer.SetRow(00, "== TANKS! ==");
        _scoresBuffer.SetRow(01, "-- scores --");
        _scoresBuffer.SetRow(17, "--  join  --");
        _scoresBuffer.SetRow(18, string.Join('.', localIp[..2]));
        _scoresBuffer.SetRow(19, string.Join('.', localIp[2..]));
    }

    public async Task OnFrameDoneAsync(GamePixelGrid gamePixelGrid, Bitmap observerPixels)
    {
        if (!_options.CurrentValue.EnableServicePointDisplay)
            return;

        if (DateTime.Now < _nextFrameAfter)
            return;

        _nextFrameAfter = DateTime.Now + _minFrameTime;
        await Task.Yield();

        RefreshScores();

        try
        {
            _displayConnection.Send(Command.BitmapLinearWin(0, 0, observerPixels, CompressionCode.Lzma));
            _displayConnection.Send(Command.Cp437Data(MapService.TilesPerRow, 0, _scoresBuffer.ToCp437()));
        }
        catch (SocketException ex)
        {
            if (DateTime.Now > _nextFailLogAfter)
            {
                _logger.LogWarning("could not send data to service point display: {}", ex.Message);
                _nextFailLogAfter = DateTime.Now + TimeSpan.FromSeconds(5);
            }
        }
    }

    private void RefreshScores()
    {
        var playersToDisplay = _players.Players
            .OrderByDescending(p => p.Scores.Kills)
            .Take(ScoresPlayerRows);

        ushort row = 2;
        foreach (var p in playersToDisplay)
        {
            var score = p.Scores.Kills.ToString();
            var nameLength = Math.Min(p.Name.Length, ScoresWidth - score.Length - 1);

            var name = p.Name[..nameLength];
            var spaces = new string(' ', ScoresWidth - score.Length - nameLength);

            _scoresBuffer.SetRow(row, name + spaces + score);
            row++;
        }

        for (; row < 16; row++)
            _scoresBuffer.SetRow(row, new string(' ', ScoresWidth));

        _scoresBuffer.SetRow(16, _mapService.Current.Name[..(Math.Min(ScoresWidth, _mapService.Current.Name.Length) - 1)]);
    }

    private static string GetLocalIPv4(DisplayConfiguration configuration)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
        socket.Connect(configuration.Hostname, configuration.Port);
        var endPoint = socket.LocalEndPoint as IPEndPoint ?? throw new NotSupportedException();
        return endPoint.Address.ToString();
    }

    public void Dispose()
    {
        _displayConnection.Dispose();
        _scoresBuffer.Dispose();
    }
}
