using NetBank.Common;
using NetBank.Common.Structures;
using NetBank.Common.Structures.Caching;
using NetBank.Errors;
using NetBank.Persistence;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public class StorageBufferProcessor : IProcessor<AccountServiceCaptureBuffer>
{
    private readonly IStorageStrategy _storageStrategy;
    private readonly Cache<AccountIdentifier, Account> _cache; // Now caches the Account entity
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    
    public StorageBufferProcessor(IStorageStrategy storageStrategy)
    {
        _storageStrategy = storageStrategy;
        _cache = new Cache<AccountIdentifier, Account>(100, new LruEvictionPolicy<AccountIdentifier>());
    }

    public async Task Flush(AccountServiceCaptureBuffer capture)
    {
        await _flushLock.WaitAsync();
        try
        {
            // 1. Sync cache with current persistent state
            await PrefetchMissingAccountsIntoCache(capture.TouchedAccounts);
            
            // 2. Handle Creations
            await ProcessCreations(capture.CreationOperations);
            
            // 3. Process Removals (Logic: Amount must be 0)
            await ProcessRemovals(capture.RemoveOperations);

            // 4. Process Financial Updates (Deposits and Withdrawals)
            await ProcessFinancialOperations(capture.DepositOperations, capture.WithdrawOperations);

            // 5. Global Stats & Queries
            await ResolveBankTotalRequests(capture.BankTotalRequests);
            await ResolveBankClientCountRequests(capture.ClientNumberRequests);
            ResolveBalanceRequests(capture.BalanceRequests);
        }
        finally
        {
            _cache.Clear();
            capture.Clear();
            _flushLock.Release();
        }
    }

    private readonly List<AccountIdentifier> _prefetchList = new();
    private async Task PrefetchMissingAccountsIntoCache(HashSet<AccountIdentifier> missingIds)
    {
        _prefetchList.Clear();
        
        // This is slower but is neccessary to prevent required ids from eviction by touching them
        missingIds.RemoveWhere(id => _cache.Contains(id));
        
        if (missingIds.Count == 0) return;
        
        foreach (var identifier in missingIds)
        {
            _prefetchList.Add(identifier);
        }
        
        AdjustCacheSize(_cache.Count + missingIds.Count);

        var fetchedAccounts = await _storageStrategy.GetAll(_prefetchList);
    
        foreach (var account in fetchedAccounts)
        {
            _cache.Set(account.Identifier, account);
        }
    }

    private async Task ProcessCreations(IReadOnlyList<TaskCompletionSource<AccountIdentifier>> ops)
    {
        if (ops.Count == 0) return;

        var createdIds = await _storageStrategy.CreateAccounts(ops.Count);
        for (int i = 0; i < ops.Count; i++)
        {
            if (i < createdIds.Count)
            {
                var id = createdIds[i];
                var newAccount = new Account(id, new Amount(0));
                _cache.Set(id, newAccount);
                ops[i].TrySetResult(id);
            }
            else
            {
                ops[i].TrySetException(CreateException(ErrorOrigin.System, "Capacity reached."));
            }
        }
    }
    
    private readonly List<AccountIdentifier> _toRemove = new();
    private readonly List<TaskCompletionSource> _tcsList = new();

    private async Task ProcessRemovals(IEnumerable<(TaskCompletionSource tcs, AccountIdentifier id)> ops)
    {
        _toRemove.Clear();
        _tcsList.Clear();
        
        foreach (var (tcs, id) in ops)
        {
            if (_cache.TryGet(id, out var account))
            {
                if (account.CanBeDeleted())
                {
                    _toRemove.Add(id);
                    _tcsList.Add(tcs);
                    _cache.Remove(id);
                }
                else
                {
                    tcs.TrySetException(CreateException(ErrorOrigin.Client, "Cannot delete account with remaining balance."));
                }
            }
            else
            {
                tcs.TrySetException(CreateException(ErrorOrigin.Client, "Account not found."));
            }
        }

        if (_toRemove.Count > 0)
        {
            await _storageStrategy.RemoveAccounts(_toRemove);
            foreach (var tcs in _tcsList) tcs.TrySetResult();
        }
    }
    
    private readonly List<Account> _dirtyAccounts = new();
    private readonly List<TaskCompletionSource> _successfulTcs = new();

    private async Task ProcessFinancialOperations(
        IEnumerable<(TaskCompletionSource tcs, AccountIdentifier id, Amount amount)> deposits,
        IEnumerable<(TaskCompletionSource tcs, AccountIdentifier id, Amount amount)> withdrawals)
    {
        _dirtyAccounts.Clear();
        _successfulTcs.Clear();

        foreach (var (tcs, id, amount) in deposits)
        {
            if (_cache.TryGet(id, out var account))
            {
                account.Deposit(amount);
                _dirtyAccounts.Add(account);
                _successfulTcs.Add(tcs);
            }
            else
            {
                tcs.TrySetException(CreateException(ErrorOrigin.Client, "Account not found."));
            }
        }
        
        foreach (var (tcs, id, amount) in withdrawals)
        {
            if (_cache.TryGet(id, out var account))
            {
                try 
                {
                    account.Withdraw(amount);
                    _dirtyAccounts.Add(account);
                    _successfulTcs.Add(tcs);
                }
                catch (ArgumentException ex)
                {
                    tcs.TrySetException(CreateException(ErrorOrigin.Client, ex.Message));
                }
            }
            else
            {
                tcs.TrySetException(CreateException(ErrorOrigin.Client, "Account not found."));
            }
        }

        if (_dirtyAccounts.Count > 0)
        {
            await _storageStrategy.UpdateAll(_dirtyAccounts);
            foreach (var tcs in _successfulTcs) tcs.TrySetResult();
        }
    }

    private void ResolveBalanceRequests(IEnumerable<(TaskCompletionSource<Amount>, AccountIdentifier)> requests)
    {
        foreach (var (tcs, id) in requests)
        {
            if (_cache.TryGet(id, out var account))
                tcs.TrySetResult(account.Amount);
            else
                tcs.TrySetException(CreateException(ErrorOrigin.Client, "Account not found."));
        }
    }

    // Helper logic for BankTotal and ClientCount remains similar as they query the Strategy directly
    private async Task ResolveBankTotalRequests(IEnumerable<TaskCompletionSource<Amount>> requests)
    {
        var total = await _storageStrategy.BankTotal();
        foreach (var tcs in requests) tcs.TrySetResult(total);
    }

    private async Task ResolveBankClientCountRequests(IEnumerable<TaskCompletionSource<int>> requests)
    {
        var count = await _storageStrategy.BankNumberOfClients();
        foreach (var tcs in requests) tcs.TrySetResult(count);
    }

    private void AdjustCacheSize(int desiredSize)
    {
        var target = Math.Max(desiredSize, MinimumCacheSize);
        if (_cache.MaximumCapacity < target || target * 2 < _cache.MaximumCapacity)
        {
            _cache.MaximumCapacity = target;
        }
    }

    private const int MinimumCacheSize = 100;
    private static ModuleException CreateException(ErrorOrigin origin, string message) =>
        new(new ModuleErrorIdentifier(Module.StorageProcessor), origin, message);
}