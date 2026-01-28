using System.Collections.Concurrent;
using NetBank.NetworkScan;

namespace NetBank.Services.NetworkScan;

public class InMemoryScanProgressStrategy : IScanProgressStore
{
    private readonly ConcurrentDictionary<string, ScanProgress> _updates = new();

    public void Add(ScanProgress update)
    {
        var key = $"{update.Ip}:{update.Port}";
        _updates[key] = update;
    }

    public IReadOnlyList<ScanProgress> GetAll()
        => _updates.Values.OrderBy(u => u.Ip).ToList();

    public void Clear()
        => _updates.Clear();
}