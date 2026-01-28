using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetBank.NetworkScan;
using NetBank.Services.NetworkScan;

namespace NetBank.Services.Implementations;

public class NetworkScanService : INetworkScanService
{
    private static readonly byte[] ProbeMessage = Encoding.ASCII.GetBytes("BC ");
    private readonly IScanProgressStore _store;
    private readonly ILogger<NetworkScanService> _logger;
    private readonly List<WebSocket> _clients = new();
    private bool isScanning = false;

    public NetworkScanService(IScanProgressStore store,ILogger<NetworkScanService> logger)
    {
        _store = store;
        _logger = logger;

    }
    // Add a new WebSocket connection
    public void AddWebSocketClient(WebSocket socket)
    {
        lock (_clients)
        {
            _clients.Add(socket);
        }
    }

    // Remove disconnected sockets
    public void RemoveWebSocketClient(WebSocket socket)
    {
        lock (_clients)
        {
            _clients.Remove(socket);
        }
    }

  private async Task BroadcastEventAsync(string type, object? payload = null)
    {
        var evt = new ScanEvent(type, payload);
        var json = JsonSerializer.Serialize(evt);
        var buffer = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(buffer);

        List<WebSocket> closedSockets = new();

        lock (_clients)
        {
            foreach (var socket in _clients)
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        socket.SendAsync(
                            segment,
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        ).Wait();
                    }
                    catch
                    {
                        closedSockets.Add(socket);
                    }
                }
                else
                {
                    closedSockets.Add(socket);
                }
            }

            foreach (var s in closedSockets)
                _clients.Remove(s);
        }
    }

    private async Task UpdateProgress(ScanProgress progress)
    {
        _store.Add(progress);
        _logger.LogInformation("{Ip}:{Port} = {Status}",
            progress.Ip, progress.Port, progress.Status);

        await BroadcastEventAsync("progress", progress);
    }

    public async Task StartScanAsync(ScanRequest request, CancellationToken ct = default)
    {
        if (isScanning)
            return;

        isScanning = true;

        _logger.LogInformation(
            "Starting network scan from {StartIp} to {EndIp} on port {Port}",
            request.IpRangeStart,
            request.IpRangeEnd,
            request.Port
        );

        _store.Clear();

        var startIp = IPAddress.Parse(request.IpRangeStart);
        var endIp = IPAddress.Parse(request.IpRangeEnd);

        foreach (var ip in EnumerateIps(startIp, endIp))
        {
            if (ct.IsCancellationRequested)
            {
                await BroadcastEventAsync("cancelled");
                _logger.LogInformation("Scan cancelled.");
                break;
            }

            await ScanIpAsync(ip, request, ct);
        }

        isScanning = false;

        await BroadcastEventAsync("completed", new
        {
            finishedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Network scan completed.");
    }

    private async Task ScanIpAsync(IPAddress ip, ScanRequest request, CancellationToken ct)
    {
        var progress = new ScanProgress(ip.ToString(), request.Port, "scanning", null);
        await UpdateProgress(progress);

        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(ip, request.Port);

            if (await Task.WhenAny(connectTask, Task.Delay(request.TimeoutMs, ct)) != connectTask)
            {
                await UpdateProgress(progress with { Status = "timeout" });
                return;
            }

            using var stream = client.GetStream();
            await stream.WriteAsync(ProbeMessage, ct);

            var buffer = new byte[256];
            var readTask = stream.ReadAsync(buffer, ct).AsTask();

            if (await Task.WhenAny(readTask, Task.Delay(request.TimeoutMs, ct)) != readTask)
            {
                await UpdateProgress(progress with { Status = "timeout" });
                return;
            }

            var response = Encoding.ASCII.GetString(buffer, 0, readTask.Result);
            await UpdateProgress(progress with { Status = "found", Response = response });
        }
        catch (Exception ex)
        {
            await UpdateProgress(progress with { Status = "error", Response = ex.Message });
        }
    }

    private static IEnumerable<IPAddress> EnumerateIps(IPAddress start, IPAddress end)
    {
        var startBytes = start.GetAddressBytes();
        var endBytes = end.GetAddressBytes();

        Array.Reverse(startBytes);
        Array.Reverse(endBytes);

        var startInt = BitConverter.ToUInt32(startBytes);
        var endInt = BitConverter.ToUInt32(endBytes);

        for (uint i = startInt; i <= endInt; i++)
        {
            var bytes = BitConverter.GetBytes(i);
            Array.Reverse(bytes);
            yield return new IPAddress(bytes);
        }
    }
}