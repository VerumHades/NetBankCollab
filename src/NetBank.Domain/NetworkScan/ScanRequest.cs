using System.ComponentModel;

namespace NetBank.NetworkScan;

public record ScanRequest
{
    [DefaultValue("192.168.0.1")]
    public string IpRangeStart { get; init; } = "192.168.0.1";

    [DefaultValue("192.168.2.1")]
    public string IpRangeEnd { get; init; } = "192.168.2.1";

    [DefaultValue(5000)]
    public int Port { get; init; } = 5000;

    [DefaultValue(5000)]
    public int TimeoutMs { get; init; } = 5000;
    
}