using System.Text.Json.Serialization;
using NetBank.Errors;

namespace NetBank;

public class Account
{
    [JsonPropertyName("identifier")]  
    public AccountIdentifier Identifier { get; private set; }
    [JsonPropertyName("amount")]
    public Amount Amount { get; private set; }
    
    public Account(AccountIdentifier identifier, Amount amount)
    {
        Identifier = identifier;
        Amount = amount;
    }

    public bool CanBeDeleted()
    {
        return Amount == new Amount(0);
    }

    public void Withdraw(Amount amount)
    {
        if (amount > Amount)
        {
            throw new ModuleException(new InsufficientFundsError(Identifier, amount, Amount), ErrorOrigin.Client, $"Cannot withdraw {amount} from {Identifier}");
        }

        Amount -= amount;
    }
    
    public void Deposit(Amount amount)
    {
        Amount += amount;
    }
}