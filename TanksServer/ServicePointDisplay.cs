using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace TanksServer;

internal class ServicePointDisplay(IOptions<ServicePointDisplayConfiguration> options)
{
    private readonly UdpClient _udpClient = new(options.Value.Hostname, options.Value.Port);

    public ValueTask<int> Send(DisplayPixelBuffer buffer)
    {
        return _udpClient.SendAsync(buffer.Data);
    }
}

internal class ServicePointDisplayConfiguration
{
    public string Hostname { get; set; } = string.Empty;
    public int Port { get; set; }
}
