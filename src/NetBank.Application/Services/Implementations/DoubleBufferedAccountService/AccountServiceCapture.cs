using NetBank.Errors;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public class AccountServiceCapture : IDisposable
{
    private bool _disposed;

    public HashSet<AccountIdentifier> TouchedAccounts { get; } = [];
    public List<TaskCompletionSource<AccountIdentifier>> CreationOperations { get; } = [];
    public List<(TaskCompletionSource tcs, AccountIdentifier id, Amount amount)> DepositOperations { get; } = [];
    public List<(TaskCompletionSource tcs, AccountIdentifier id, Amount amount)> WithdrawOperations { get; } = [];
    public List<(TaskCompletionSource tcs, AccountIdentifier id)> RemoveOperations { get; } = [];
    public List<(TaskCompletionSource<Amount> tcs, AccountIdentifier id)> BalanceRequests { get; } = [];
    public List<TaskCompletionSource<Amount>> BankTotalRequests { get; } = [];
    public List<TaskCompletionSource<int>> ClientNumberRequests { get; } = [];

    public bool HasPending => 
        CreationOperations.Count > 0 || DepositOperations.Count > 0 || 
        WithdrawOperations.Count > 0 || RemoveOperations.Count > 0 || 
        BalanceRequests.Count > 0 || BankTotalRequests.Count > 0 || 
        ClientNumberRequests.Count > 0;

    /// <summary>
    /// Fails all pending operations and clears the internal collections.
    /// </summary>
    public void Clear()
    {
        var ex = new ModuleException(
            new BufferFlushClearedUnfinishedError(), ErrorOrigin.System, _disposed ? "Buffer has been disposed." : "Buffer cleared before operations were resolved.");
        
        foreach (var tcs in CreationOperations) tcs.TrySetException(ex);
        foreach (var op in DepositOperations) op.tcs.TrySetException(ex);
        foreach (var op in WithdrawOperations) op.tcs.TrySetException(ex);
        foreach (var op in RemoveOperations) op.tcs.TrySetException(ex);
        foreach (var op in BalanceRequests) op.tcs.TrySetException(ex);
        foreach (var tcs in BankTotalRequests) tcs.TrySetException(ex);
        foreach (var tcs in ClientNumberRequests) tcs.TrySetException(ex);

        TouchedAccounts.Clear();
        CreationOperations.Clear();
        DepositOperations.Clear();
        WithdrawOperations.Clear();
        RemoveOperations.Clear();
        BalanceRequests.Clear();
        BankTotalRequests.Clear();
        ClientNumberRequests.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        Clear();
        
        GC.SuppressFinalize(this);
    }
}