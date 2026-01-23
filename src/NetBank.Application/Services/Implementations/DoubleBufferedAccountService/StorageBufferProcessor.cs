using NetBank.Common;
using NetBank.Common.Structures.Caching;
using NetBank.Errors;
using NetBank.Persistence;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public class StorageBufferProcessor : IProcessor<AccountServiceCaptureBuffer>
{
    private readonly IStorageStrategy _storageStrategy;
    private readonly Cache<AccountIdentifier, Amount> _cache;

    public StorageBufferProcessor(IStorageStrategy storageStrategy)
    {
        _storageStrategy = storageStrategy;
        _cache = new Cache<AccountIdentifier, Amount>(100, new LruEvictionPolicy<AccountIdentifier>());
    }

    public async Task Flush(AccountServiceCaptureBuffer capture)
    {
        try
        {
            await PrefetchAccountsIntoCache(capture.TouchedAccounts);
            await CreateAccounts(capture.CreationOperations);
            
            var (remData, remTcs) = FilterValidRemovals(capture.RemoveOperations);
            if (remData.Count > 0)
            {
                await _storageStrategy.RemoveAccounts(remData);
                foreach (var tcs in remTcs) tcs.TrySetResult();
            }

            var (depData, depTcs) = FilterValidDeposits(capture.DepositOperations);
            if (depData.Count > 0)
            {
                await _storageStrategy.DepositAll(depData);
                foreach (var tcs in depTcs) tcs.TrySetResult();
            }

            var (witData, witTcs) = FilterValidWithdrawals(capture.WithdrawOperations);
            if (witData.Count > 0)
            {
                await _storageStrategy.WithdrawAll(witData);
                foreach (var tcs in witTcs) tcs.TrySetResult();
            }

            await ResolveBankTotalRequests(capture.BankTotalRequests);
            await ResolveBankClientCountRequests(capture.ClientNumberRequests);
            ResolveBalanceRequests(capture.BalanceRequests);
        }
        finally
        {
            capture.Clear();
        }
    }
    
    private const int MinimumCacheSize = 100; 

    private void AdjustCacheSize(int desiredSize)
    {
        var target = Math.Max(desiredSize, MinimumCacheSize);
        if (_cache.MaximumCapacity < target || target * 2 < _cache.MaximumCapacity)
        {
            _cache.MaximumCapacity = target;
        }
    }
    
    private async Task PrefetchAccountsIntoCache(IEnumerable<AccountIdentifier> touchedAccounts)
    {
        var accountList = touchedAccounts.ToList();
        AdjustCacheSize(accountList.Count);

        var accounts = await _storageStrategy.BalanceAll(accountList);
        foreach (var (account, amount) in accounts)
            _cache.Set(account, amount);
    }

    private async Task CreateAccounts(IReadOnlyList<TaskCompletionSource<AccountIdentifier>> creationOps)
    {
        var accounts = await _storageStrategy.CreateAccounts(creationOps.Count);
        var accountList = accounts.ToList();

        for (int i = 0; i < creationOps.Count; i++)
        {
            if (i < accountList.Count){
                var id = accountList[i];
                creationOps[i].SetResult(id);
                _cache.Set(id, new Amount(0));
            }
            else
                creationOps[i].TrySetException(CreateException(ErrorOrigin.System, "Failed to create account."));
        }
    }

    private (List<AccountAndAmount> data, List<TaskCompletionSource> tcsList) FilterValidDeposits(IEnumerable<(TaskCompletionSource tcs, AccountIdentifier id, Amount amount)> ops)
    {
        var data = new List<AccountAndAmount>();
        var tcsList = new List<TaskCompletionSource>();

        foreach (var (tcs, id, amount) in ops)
        {
            if (_cache.TryGet(id, out Amount balance))
            {
                _cache.Set(id, balance + amount);
                data.Add(new AccountAndAmount(id, amount));
                tcsList.Add(tcs);
            }
            else
            {
                tcs.TrySetException(CreateException(ErrorOrigin.Client, "Account does not exist."));
            }
        }
        return (data, tcsList);
    }

    private (List<AccountAndAmount> data, List<TaskCompletionSource> tcsList) FilterValidWithdrawals(IEnumerable<(TaskCompletionSource tcs, AccountIdentifier id, Amount amount)> ops)
    {
        var data = new List<AccountAndAmount>();
        var tcsList = new List<TaskCompletionSource>();

        foreach (var (tcs, id, amount) in ops)
        {
            if (!_cache.TryGet(id, out var balance))
            {
                tcs.TrySetException(CreateException(ErrorOrigin.Client, "Account does not exist."));
            }
            else if (balance < amount)
            {
                tcs.TrySetException(CreateException(ErrorOrigin.Client, "Insufficient funds."));
            }
            else
            {
                _cache.Set(id, balance - amount);
                data.Add(new AccountAndAmount(id, amount));
                tcsList.Add(tcs);
            }
        }
        return (data, tcsList);
    }

    private (List<AccountIdentifier> data, List<TaskCompletionSource> tcsList) FilterValidRemovals(IEnumerable<(TaskCompletionSource tcs, AccountIdentifier id)> ops)
    {
        var data = new List<AccountIdentifier>();
        var tcsList = new List<TaskCompletionSource>();

        foreach (var (tcs, id) in ops)
        {
            if (_cache.TryGet(id, out _))
            {
                _cache.Remove(id);
                data.Add(id);
                tcsList.Add(tcs);
            }
            else
            {
                tcs.TrySetException(CreateException(ErrorOrigin.Client, "Account does not exist."));
            }
        }
        return (data, tcsList);
    }

    private async Task ResolveBankTotalRequests(IEnumerable<TaskCompletionSource<Amount>> requests)
    {
        var total = await _storageStrategy.BankTotal();
        foreach (var tcs in requests) tcs.SetResult(total);
    }

    private async Task ResolveBankClientCountRequests(IEnumerable<TaskCompletionSource<int>> requests)
    {
        var count = await _storageStrategy.BankNumberOfClients();
        foreach (var tcs in requests) tcs.SetResult(count);
    }

    private void ResolveBalanceRequests(IEnumerable<(TaskCompletionSource<Amount>, AccountIdentifier)> requests)
    {
        foreach (var (tcs, id) in requests)
        {
            if (_cache.TryGet(id, out var amount))
                tcs.TrySetResult(amount); // Use Try
            else
                tcs.TrySetException(CreateException(ErrorOrigin.Client, "Account does not exist."));
        }
    }

    private static ModuleException CreateException(ErrorOrigin origin, string message) =>
        new(new ModuleErrorIdentifier(Module.StorageProcessor), origin, message);
    
}