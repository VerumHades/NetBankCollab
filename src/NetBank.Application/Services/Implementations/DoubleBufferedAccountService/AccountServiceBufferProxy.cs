namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public class AccountServiceBufferProxy(DoubleBufferedAccountCoordinator coordinator) : IAccountService
{
    public Action? OnActivity { get; set; }
    
    private IAccountService CurrentWriter
    {
        get
        {
            OnActivity?.Invoke();
            return coordinator.GetWriter();
        }
    }

    public Task<AccountIdentifier> CreateAccount() => CurrentWriter.CreateAccount();
    
    public Task Deposit(AccountIdentifier account, Amount amount) 
        => CurrentWriter.Deposit(account, amount);

    public Task Withdraw(AccountIdentifier account, Amount amount) 
        => CurrentWriter.Withdraw(account, amount);

    public Task RemoveAccount(AccountIdentifier account) 
        => CurrentWriter.RemoveAccount(account);

    public Task<Amount> Balance(AccountIdentifier account) 
        => CurrentWriter.Balance(account);

    public Task<Amount> BankTotal() => CurrentWriter.BankTotal();

    public Task<int> BankNumberOfClients() => CurrentWriter.BankNumberOfClients();
}