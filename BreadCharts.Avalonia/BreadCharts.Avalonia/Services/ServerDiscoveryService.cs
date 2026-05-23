using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BreadCharts.Avalonia.Services;

public record ServerInfo(string App, string Address, string MachineName);

public class ServerDiscoveryService : IDisposable
{
    private readonly int _port = 50001;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;
    public ObservableCollection<ServerInfo> DiscoveredServers { get; } = new();

    public void StartListening()
    {
        if (_udpClient != null) return;

        try
        {
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.ExclusiveAddressUse = false;
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, _port));

            _cts = new CancellationTokenSource();
            Task.Run(() => ListenLoop(_cts.Token));
        }
        catch (PlatformNotSupportedException)
        {
            // UDP is not supported on this platform (e.g. WASM)
            _udpClient = null;
        }
        catch (Exception ex)
        {
            // Other errors
            _udpClient = null;
        }
    }

    private async Task ListenLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient!.ReceiveAsync(token);
                var message = Encoding.UTF8.GetString(result.Buffer);
                var serverInfo = JsonSerializer.Deserialize<ServerInfo>(message);

                if (serverInfo != null && serverInfo.App == "BreadCharts")
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        bool exists = false;
                        foreach (var s in DiscoveredServers)
                        {
                            if (s.Address == serverInfo.Address)
                            {
                                exists = true;
                                break;
                            }
                        }

                        if (!exists)
                        {
                            DiscoveredServers.Add(serverInfo);
                        }
                    });
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Log error
            }
        }
    }

    public void StopListening()
    {
        _cts?.Cancel();
        _udpClient?.Dispose();
        _udpClient = null;
    }

    public void Dispose()
    {
        StopListening();
    }
}
