using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using NetBank.NetworkScan;
using NetBank.Services.NetworkScan;

namespace NetBank.Services.Implementations;

public class NetworkScanService : INetworkScanService
{
    private static readonly byte[] ProbeMessage = Encoding.ASCII.GetBytes("BC ");
    private readonly HttpClient _http;
    private readonly IScanProgressStore _store;

    public NetworkScanService(HttpClient httpClient,IScanProgressStore store)
    {
        _store = store;
        _http= httpClient;
    }

        public async Task StartScanAsync(ScanRequest request, CancellationToken ct = default)
    {
        _store.Clear(); // clear previous scan results

        var startIp = IPAddress.Parse(request.IpRangeStart);
        var endIp = IPAddress.Parse(request.IpRangeEnd);

        foreach (var ip in EnumerateIps(startIp, endIp))
        {
            if (ct.IsCancellationRequested)
                break;

            await ScanIpAsync(ip, request, ct);
        }
    }

    private async Task ScanIpAsync(IPAddress ip, ScanRequest request, CancellationToken ct)
    {
        var progress = new ScanProgress(ip.ToString(), request.Port, "scanning", null);
        await UpdateProgress(request, progress);

        try
        {
            using var client = new TcpClient();

            var connectTask = client.ConnectAsync(ip, request.Port);

            if (await Task.WhenAny(connectTask, Task.Delay(request.TimeoutMs, ct)) != connectTask)
            {
                await UpdateProgress(request, progress with { Status = "timeout" });
                return;
            }

            using var stream = client.GetStream();
            await stream.WriteAsync(ProbeMessage, ct);

            var buffer = new byte[256];
            var readTask = stream.ReadAsync(buffer, ct).AsTask();

            if (await Task.WhenAny(readTask, Task.Delay(request.TimeoutMs, ct)) != readTask)
            {
                await UpdateProgress(request, progress with { Status = "timeout" });
                return;
            }

            var response = Encoding.ASCII.GetString(buffer, 0, readTask.Result);
            await UpdateProgress(request, progress with { Status = "found", Response = response });
        }
        catch (Exception ex)
        {
            await UpdateProgress(request, progress with { Status = "error", Response = ex.Message });
        }
    }

    private async Task UpdateProgress(ScanRequest request, ScanProgress progress)
    {
        _store.Add(progress); // save to store for polling

        if (!string.IsNullOrWhiteSpace(request.WebhookUrl))
        {
            try
            {
                await _http.PostAsJsonAsync(request.WebhookUrl, progress);
            }
            catch
            {
                // silently ignore webhook errors
            }
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