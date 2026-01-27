using NetBank.NetworkScan;

namespace NetBank.Services.NetworkScan;

public interface IScanProgressStore
{
    void Add(ScanProgress update);
    IReadOnlyList<ScanProgress> GetAll();
    void Clear();
}