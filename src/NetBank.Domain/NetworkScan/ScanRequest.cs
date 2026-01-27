namespace NetBank.NetworkScan;

public record ScanRequest(
    string IpRangeStart,
    string IpRangeEnd,
    int Port,
    int TimeoutMs,
    string WebhookUrl
);