namespace NetBank.Controllers.TcpController.Parsing;

public class BankCodeDto { }
public class CreateAccountDto { }

public class DepositDto
{
    public int Account { get; set; } = -1;
    public string Ip { get; set; } = string.Empty; 
    public long Amount { get; set; } 
}

public class WithdrawDto
{
    public int Account { get; set; } = -1;
    public string Ip { get; set; } = string.Empty; 
    public long Amount { get; set; }
}

public class BalanceDto
{
    public int Account { get; set; } = -1;
    public string Ip { get; set; } = string.Empty; 
}

public class RemoveAccountDto
{
    public int Account { get; set; } = -1;
    public string Ip { get; set; } = string.Empty; 
}

public class BankTotalDto { }
public class BankClientsDto { }