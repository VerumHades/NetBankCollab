using System.Collections.Concurrent;
using NetBank.Persistence;

namespace NetBank.Infrastructure;

public class InMemoryStorageStrategy : IStorageStrategy
{
    private readonly ConcurrentDictionary<AccountIdentifier, Amount> _balances = new();
    
    private readonly ConcurrentQueue<AccountIdentifier> _recycledIds = new();
    
    private int _highestIssuedId = 9999; 
    private readonly object _idLock = new();

    public Task<IReadOnlyList<AccountIdentifier>> CreateAccounts(int count)
    {
        var created = new List<AccountIdentifier>();

        for (int i = 0; i < count; i++)
        {
            if (_recycledIds.TryDequeue(out var recycledId))
            {
                if (_balances.TryAdd(recycledId, new Amount(0)))
                {
                    created.Add(recycledId);
                    continue;
                }
            }

            lock (_idLock)
            {
                if (_highestIssuedId < 99999)
                {
                    var newId = new AccountIdentifier(++_highestIssuedId);
                    if (_balances.TryAdd(newId, new Amount(0)))
                    {
                        created.Add(newId);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        return Task.FromResult<IReadOnlyList<AccountIdentifier>>(created);
    }

    public Task<IReadOnlyList<AccountIdentifier>> RemoveAccounts(IEnumerable<AccountIdentifier> accounts)
    {
        var removed = new List<AccountIdentifier>();
        foreach (var id in accounts)
        {
            if (_balances.TryRemove(id, out _))
            {
                removed.Add(id);
                // Return the ID to the pool for future use
                _recycledIds.Enqueue(id);
            }
        }
        return Task.FromResult<IReadOnlyList<AccountIdentifier>>(removed);
    }

    public Task<IReadOnlyList<AccountIdentifier>> DepositAll(IEnumerable<AccountAndAmount> amounts)
    {
        var updated = new List<AccountIdentifier>();
        foreach (var item in amounts)
        {
            // AddOrUpdate ensures thread-safety for the balance arithmetic
            _balances.AddOrUpdate(
                item.Account,
                _ => item.Amount, 
                (_, current) => current + item.Amount);
            
            updated.Add(item.Account);
        }
        return Task.FromResult<IReadOnlyList<AccountIdentifier>>(updated);
    }

    public Task<IReadOnlyList<AccountIdentifier>> WithdrawAll(IEnumerable<AccountAndAmount> amounts)
    {
        var updated = new List<AccountIdentifier>();
        foreach (var item in amounts)
        {
            if (_balances.ContainsKey(item.Account))
            {
                _balances.AddOrUpdate(
                    item.Account,
                    _ => new Amount(0),
                    (_, current) => current - item.Amount);
                
                updated.Add(item.Account);
            }
        }
        return Task.FromResult<IReadOnlyList<AccountIdentifier>>(updated);
    }

    public Task<IReadOnlyList<AccountAndAmount>> BalanceAll(IEnumerable<AccountIdentifier> accounts)
    {
        var results = new List<AccountAndAmount>();
        foreach (var id in accounts)
        {
            if (_balances.TryGetValue(id, out var balance))
            {
                results.Add(new AccountAndAmount(id, balance));
            }
        }
        return Task.FromResult<IReadOnlyList<AccountAndAmount>>(results);
    }

    public Task<Amount> BankTotal()
    {
        var total = _balances.Values.Aggregate(new Amount(0), (sum, next) => sum + next);
        return Task.FromResult(total);
    }

    public Task<int> BankNumberOfClients()
    {
        return Task.FromResult(_balances.Count);
    }
}