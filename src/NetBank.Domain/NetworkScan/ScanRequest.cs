using System.ComponentModel;

namespace NetBank.NetworkScan;

public record ScanRequest
{
    [DefaultValue("192.168.0.1")]
    public string IpRangeStart { get; init; } = "10.2.7.127";

    [DefaultValue("192.168.0.1")]
    public string IpRangeEnd { get; init; } = "10.2.7.150";

    [DefaultValue(65525)]
    public int Port { get; init; } = 65525;
    
    [DefaultValue(65525)]
    public int PortRangeStart { get; init; } = 65525;
    [DefaultValue(65535)]
    public int PortRangeEnd { get; init; } = 65535;

    [DefaultValue(5000)]
    public int TimeoutMs { get; init; } = 5000;
    
}