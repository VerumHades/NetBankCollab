namespace NetBank.NetworkScan;

public record ScanProgress(
    string Ip,
    int Port,
    string Status, // scanning | found | timeout | error
    string? Response
);