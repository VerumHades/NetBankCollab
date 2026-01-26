namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public class AccountServiceBufferWriter(AccountServiceCapture buffer) : IAccountService
{

    public Task<AccountIdentifier> CreateAccount() 
        => Deferred<AccountIdentifier>(tcs => buffer.CreationOperations.Add(tcs));

    public Task RemoveAccount(AccountIdentifier account) => Deferred(tcs => {
        buffer.TouchedAccounts.Add(account);
        buffer.RemoveOperations.Add((tcs, account));
    });

    public Task Deposit(AccountIdentifier account, Amount amount) => Deferred(tcs => {
        buffer.TouchedAccounts.Add(account);
        buffer.DepositOperations.Add((tcs, account, amount));
    });

    public Task Withdraw(AccountIdentifier account, Amount amount) => Deferred(tcs => {
        buffer.TouchedAccounts.Add(account);
        buffer.WithdrawOperations.Add((tcs, account, amount));
    });

    public Task<Amount> Balance(AccountIdentifier account) => Deferred<Amount>(tcs => {
        buffer.TouchedAccounts.Add(account);
        buffer.BalanceRequests.Add((tcs, account));
    });

    public Task<Amount> BankTotal() 
        => Deferred<Amount>(tcs => buffer.BankTotalRequests.Add(tcs));

    public Task<int> BankNumberOfClients() 
        => Deferred<int>(tcs => buffer.ClientNumberRequests.Add(tcs));

    private Task<T> Deferred<T>(Action<TaskCompletionSource<T>> action)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        action(tcs);
        return tcs.Task;
    }

    private Task Deferred(Action<TaskCompletionSource> action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        action(tcs);
        return tcs.Task;
    }
}