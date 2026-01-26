using NetBank.Common;
using NetBank.Common.Caching;
using NetBank.Errors;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public class CapturedAccountActionsProcessor(IStorageStrategy storageStrategy) : IProcessor<AccountServiceCapture>
{
    private readonly Cache<AccountIdentifier, Account> _cache = new(100, new LruEvictionPolicy<AccountIdentifier>()); // Now caches the Account entity
    private readonly SemaphoreSlim _flushLock = new(1, 1);

    public async Task Flush(AccountServiceCapture capture, CancellationToken cancellationToken)
    {
        await _flushLock.WaitAsync(cancellationToken);

        try
        {
            await PrefetchMissingAccountsIntoCache(capture.TouchedAccounts);
            if (cancellationToken.IsCancellationRequested) return;

            await ProcessCreations(capture.CreationOperations);
            if (cancellationToken.IsCancellationRequested) return;

            await ProcessRemovals(capture.RemoveOperations);
            if (cancellationToken.IsCancellationRequested) return;

            await ProcessFinancialOperations(capture.DepositOperations, capture.WithdrawOperations);

            if (!cancellationToken.IsCancellationRequested)
                await ResolveBankTotalRequests(capture.BankTotalRequests);

            if (!cancellationToken.IsCancellationRequested)
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

        var fetchedAccounts = await storageStrategy.GetAll(_prefetchList);
    
        foreach (var account in fetchedAccounts)
        {
            _cache.Set(account.Identifier, account);
        }
    }

    private async Task ProcessCreations(IReadOnlyList<TaskCompletionSource<AccountIdentifier>> ops)
    {
        if (ops.Count == 0) return;

        var createdIds = await storageStrategy.CreateAccounts(ops.Count);
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
                ops[i].TrySetException(
                    new ModuleException(
                        new AccountMaxCapacityReachedError(), 
                        ErrorOrigin.System, 
                        "Capacity reached.")
                    );
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
                    tcs.TrySetException(
                        new ModuleException(
                            new CannotRemoveAccountWithRemainingBalanceError(account.Amount),
                            ErrorOrigin.Client,
                            "Cannot delete account with remaining balance."
                            ));
                }
            }
            else
            {
                tcs.TrySetException(CreateAccountNotFound(id));
            }
        }

        if (_toRemove.Count > 0)
        {
            await storageStrategy.RemoveAccounts(_toRemove);
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
                tcs.TrySetException(CreateAccountNotFound(id));
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
                    tcs.TrySetException(ex);
                }
            }
            else
            {
                tcs.TrySetException(CreateAccountNotFound(id));
            }
        }

        if (_dirtyAccounts.Count > 0)
        {
            await storageStrategy.UpdateAll(_dirtyAccounts);
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
                tcs.TrySetException(CreateAccountNotFound(id));
        }
    }
    
    private async Task ResolveBankTotalRequests(IEnumerable<TaskCompletionSource<Amount>> requests)
    {
        var total = await storageStrategy.BankTotal();
        foreach (var tcs in requests) tcs.TrySetResult(total);
    }

    private async Task ResolveBankClientCountRequests(IEnumerable<TaskCompletionSource<int>> requests)
    {
        var count = await storageStrategy.BankNumberOfClients();
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

    private static ModuleException CreateAccountNotFound(AccountIdentifier id) => new ModuleException(
        new AccountNotFoundError(id),
        ErrorOrigin.Client,
        "Account not found."
    );
}