namespace NetBank;

public class Account
{
    public AccountIdentifier Identifier { get; private set; }
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
            throw new ArgumentException($"Cannot withdraw {amount} from {Identifier}");
        }

        Amount -= amount;
    }
    
    public void Deposit(Amount amount)
    {
        Amount += amount;
    }
}