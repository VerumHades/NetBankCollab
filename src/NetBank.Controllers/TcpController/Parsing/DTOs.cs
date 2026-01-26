namespace NetBank.Controllers.TcpController.Parsing;

// Simple records still work fine with parameterless defaults
public record BankCodeDto();
public record CreateAccountDto();
public record BankTotalDto();
public record BankClientsDto();

public record DepositDto
{
    public int Account { get; set; } = -1;
    public string Ip { get; set; } = string.Empty; 
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

public record WithdrawDto
{
    public int Account { get; set; } = -1;
    public string Ip { get; set; } = string.Empty; 
    public long Amount { get; set; }

    public WithdrawDto() { }

    public WithdrawDto(int account, string ip, long amount)
    {
        Account = account;
        Ip = ip;
        Amount = amount;
    }
}

public record BalanceDto
{
    public int Account { get; set; } = -1;
    public string Ip { get; set; } = string.Empty;

    public BalanceDto() { }

    public BalanceDto(int account, string ip)
    {
        Account = account;
        Ip = ip;
    }
}

public record RemoveAccountDto
{
    public int Account { get; set; } = -1;
    public string Ip { get; set; } = string.Empty;

    public RemoveAccountDto() { }

    public RemoveAccountDto(int account, string ip)
    {
        Account = account;
        Ip = ip;
    }
}