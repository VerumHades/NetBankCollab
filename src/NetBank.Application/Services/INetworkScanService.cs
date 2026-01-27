using NetBank.NetworkScan;

namespace NetBank.Services;

public interface INetworkScanService
{
    Task StartScanAsync(ScanRequest request, CancellationToken ct = default);
}