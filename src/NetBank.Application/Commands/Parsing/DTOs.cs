
namespace NetBank.Commands.Parsing;

public record WithIpDto()
{
    public string Ip { get; set; } = string.Empty; 
}
// Simple records still work fine with parameterless defaults
public record BankCodeDto();
public record CreateAccountDto();
public record BankTotalDto();
public record BankClientsDto();

public record RobberyPlanDto{
    public int Account { get; set; } = -1;
    public RobberyPlanDto() { }
    public RobberyPlanDto(int amount)
    {
        Account = amount;
    }
};

public record DepositDto: WithIpDto
{
    public int Account { get; set; } = -1;
    public long Amount { get; set; }

    // Parameterless constructor for Template<T>
    public DepositDto() { }

    // "Take-all" constructor for manual creation
    public DepositDto(int account, string ip, long amount)
    {
        Account = account;
        Ip = ip;
        Amount = amount;
    }
}

public record WithdrawDto: WithIpDto
{
    public int Account { get; set; } = -1;
    public long Amount { get; set; }

    public WithdrawDto() { }

    public WithdrawDto(int account, string ip, long amount)
    {
        Account = account;
        Ip = ip;
        Amount = amount;
    }
}

public record BalanceDto: WithIpDto
{
    public int Account { get; set; } = -1;

    public BalanceDto() { }

    public BalanceDto(int account, string ip)
    {
        Account = account;
        Ip = ip;
    }
}

public record RemoveAccountDto: WithIpDto
{
    public int Account { get; set; } = -1;

    public RemoveAccountDto() { }

    public RemoveAccountDto(int account, string ip)
    {
        Account = account;
        Ip = ip;
    }
}