namespace NetBank.Services;

public interface IAccountService
{
    Task<AccountIdentifier> CreateAccount();
    Task RemoveAccount(AccountIdentifier account);
    
    Task Deposit(AccountIdentifier account, Amount amount);
    Task Withdraw(AccountIdentifier account, Amount amount);
    Task<Amount> Balance(AccountIdentifier account);

    Task<Amount> BankTotal();
    Task<int> BankNumberOfClients();
}