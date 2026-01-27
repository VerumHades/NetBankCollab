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
    private readonly HttpClient _http;
    private readonly IScanProgressStore _store;
    private readonly ILogger<NetworkScanService> _logger;
    private readonly List<WebSocket> _clients = new();

    public NetworkScanService(HttpClient httpClient,IScanProgressStore store,ILogger<NetworkScanService> logger)
    {
        _store = store;
        _http= httpClient;
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
    private void RemoveWebSocketClient(WebSocket socket)
    {
        lock (_clients)
        {
            _clients.Remove(socket);
        }
    }

    // Broadcast progress to all WebSocket clients
    private async Task BroadcastProgressAsync(ScanProgress progress)
    {
        var json = JsonSerializer.Serialize(progress);
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
                        socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None).Wait();
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
        // Save progress to store
        _store.Add(progress);
        _logger.LogInformation("{Ip}:{Port} = {Status}", progress.Ip, progress.Port, progress.Status);

        // Broadcast to WebSocket clients
        await BroadcastProgressAsync(progress);
    }

    public async Task StartScanAsync(ScanRequest request, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting network scan from {StartIp} to {EndIp} on port {Port}", 
            request.IpRangeStart, request.IpRangeEnd, request.Port);

        _store.Clear(); // clear previous scan results
        
        var startIp = IPAddress.Parse(request.IpRangeStart);
        var endIp = IPAddress.Parse(request.IpRangeEnd);

        foreach (var ip in EnumerateIps(startIp, endIp))
        {
            if (ct.IsCancellationRequested)
            {
                _logger.LogInformation("Scan cancelled by user.");
                break;
            }

            await ScanIpAsync(ip, request, ct);
        }

        _logger.LogInformation("Network scan completed.");
    }

    private async Task ScanIpAsync(IPAddress ip, ScanRequest request, CancellationToken ct)
    {
        // Update status to scanning immediately
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