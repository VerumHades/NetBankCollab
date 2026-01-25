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
                    break; // ID space exhausted
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
                _recycledIds.Enqueue(id);
            }
        }
        return Task.FromResult<IReadOnlyList<AccountIdentifier>>(removed);
    }

    /// <summary>
    /// Synchronizes the persistent state with the entity state provided.
    /// </summary>
    public Task<IReadOnlyList<AccountIdentifier>> UpdateAll(IEnumerable<Account> accounts)
    {
        var updated = new List<AccountIdentifier>();
        foreach (var account in accounts)
        {
            // The logic has already happened in the entity; 
            // we just overwrite the persistent balance with the new total.
            _balances[account.Identifier] = account.Amount;
            updated.Add(account.Identifier);
        }
        return Task.FromResult<IReadOnlyList<AccountIdentifier>>(updated);
    }

    /// <summary>
    /// Maps the internal primitive balances back into Account entities.
    /// </summary>
    public Task<IReadOnlyList<Account>> GetAll(IEnumerable<AccountIdentifier> accounts)
    {
        var results = new List<Account>();
        foreach (var id in accounts)
        {
            if (_balances.TryGetValue(id, out var balance))
            {
                results.Add(new Account(id, balance));
            }
        }
        return Task.FromResult<IReadOnlyList<Account>>(results);
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