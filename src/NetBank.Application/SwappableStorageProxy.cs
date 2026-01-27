namespace NetBank;

public class SwappableStorageProxy : IStorageStrategy
{
    private IStorageStrategy _currentStrategy;
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

    public SwappableStorageProxy(IStorageStrategy initialStrategy)
    {
        _currentStrategy = initialStrategy;
    }

    /// <summary>
    /// Swaps the underlying storage strategy safely. 
    /// Blocks until all current operations finish, then prevents new ones until swap is done.
    /// </summary>
    public void SwapStrategy(IStorageStrategy newStrategy)
    {
        _lock.EnterWriteLock();
        try
        {
            _currentStrategy = newStrategy;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private async Task<T> ExecuteAsync<T>(Func<IStorageStrategy, Task<T>> operation)
    {
        _lock.EnterReadLock();
        try
        {
            return await operation(_currentStrategy);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    // --- Interface Implementation ---

    public Task<IReadOnlyList<AccountIdentifier>> CreateAccounts(int count) 
        => ExecuteAsync(s => s.CreateAccounts(count));

    public Task<IReadOnlyList<AccountIdentifier>> RemoveAccounts(IEnumerable<AccountIdentifier> accounts) 
        => ExecuteAsync(s => s.RemoveAccounts(accounts));

    public Task<IReadOnlyList<AccountIdentifier>> UpdateAll(IEnumerable<Account> amounts) 
        => ExecuteAsync(s => s.UpdateAll(amounts));

    public Task<IReadOnlyList<Account>> GetAll(IEnumerable<AccountIdentifier> accounts) 
        => ExecuteAsync(s => s.GetAll(accounts));

    public Task<Amount> BankTotal() 
        => ExecuteAsync(s => s.BankTotal());

    public Task<int> BankNumberOfClients() 
        => ExecuteAsync(s => s.BankNumberOfClients());
}