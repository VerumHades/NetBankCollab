using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetBank.Common;

namespace NetBank;

public class SwappableStorageProxy(IStorageStrategy initialStrategy) : IStorageStrategy
{
    private IStorageStrategy _currentStrategy = initialStrategy;
    private readonly AsyncReaderWriterLock _lock = new();

    public async Task SwapStrategy(IStorageStrategy newStrategy)
    {
        // Request exclusive access. This waits for all current ExecuteAsync 
        // calls to finish and blocks new ones from starting.
        //using (await _lock.WriterLockAsync())
        //{
            _currentStrategy = newStrategy;
        //}
    }

    private async Task<T> ExecuteAsync<T>(Func<IStorageStrategy, Task<T>> operation)
    {
        // Request shared access. Many threads can enter here at once.
        //using (await _lock.ReaderLockAsync())
        //{
            return await operation(_currentStrategy);
        //}
    }


    public Task<IReadOnlyList<AccountIdentifier>> CreateAccounts(int count) 
        => _currentStrategy.CreateAccounts(count);

    public Task<IReadOnlyList<AccountIdentifier>> RemoveAccounts(IEnumerable<AccountIdentifier> accounts) 
        => _currentStrategy.RemoveAccounts(accounts);

    public Task<IReadOnlyList<AccountIdentifier>> UpdateAll(IEnumerable<Account> amounts) 
        => _currentStrategy.UpdateAll(amounts);

    public Task<IReadOnlyList<Account>> GetAll(IEnumerable<AccountIdentifier> accounts) 
        => _currentStrategy.GetAll(accounts);

    public Task<Amount> BankTotal()
        => _currentStrategy.BankTotal();

    public Task<int> BankNumberOfClients()
        => _currentStrategy.BankNumberOfClients();
}