using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace BreadCharts.WebApi;

public class ServerDiscoveryData
{
    public string App { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string MachineName { get; set; } = null!;
}

public class ServerBroadcastService : BackgroundService
{
    private readonly IServer _server;
    private readonly ILogger<ServerBroadcastService> _logger;
    private readonly string _appName = "BreadCharts";
    private readonly int _broadcastPort = 50001;

    public ServerBroadcastService(IServer server, ILogger<ServerBroadcastService> logger)
    {
        _server = server;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for the server to start to get the addresses
        await Task.Delay(2000, stoppingToken);

        var addresses = _server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses == null || !addresses.Any())
        {
            _logger.LogWarning("No server addresses found for broadcasting.");
            return;
        }

        // Prefer HTTPS if available, otherwise take the first one
        var address = addresses.FirstOrDefault(a => a.StartsWith("https://")) ?? addresses.First();
        
        // If it's listening on all interfaces (e.g., http://[::]:5000), we need to find the local IP
        if (address.Contains("[::]") || address.Contains("0.0.0.0") || address.Contains("*"))
        {
            var port = new Uri(address.Replace("[::]", "localhost").Replace("*", "localhost")).Port;
            var localIp = GetLocalIPAddress();
            address = $"{(address.StartsWith("https") ? "https" : "http")}://{localIp}:{port}";
        }

        _logger.LogInformation("Starting server broadcast for {AppName} at {Address}", _appName, address);

        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        var endpoint = new IPEndPoint(IPAddress.Broadcast, _broadcastPort);

        var discoveryData = new ServerDiscoveryData
        {
            App = _appName,
            Address = address,
            MachineName = Environment.MachineName
        };

        var message = JsonSerializer.Serialize(discoveryData, AppJsonContext.Default.ServerDiscoveryData);
        var bytes = Encoding.UTF8.GetBytes(message);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await udpClient.SendAsync(bytes, bytes.Length, endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending broadcast message");
            }

            await Task.Delay(5000, stoppingToken);
        }
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }
}
