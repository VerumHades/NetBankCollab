using System.Net.WebSockets;
using NetBank.NetworkScan;

namespace NetBank.Services.NetworkScan;

public interface INetworkScanService
{
    Task StartScanAsync(ScanRequest request, CancellationToken ct = default);
    public void AddWebSocketClient(WebSocket socket);
}