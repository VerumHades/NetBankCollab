namespace NetBank.Controllers.TcpController.Parsing;

public static class ProtocolRegistry
{
    public static readonly Dictionary<string, object> Templates = new(StringComparer.OrdinalIgnoreCase)
    {
        { "BC", new Template<BankCodeDto>("BC") },
        { "AC", new Template<CreateAccountDto>("AC") },
        { "AD", new Template<DepositDto>("AD {Account}/{Ip} {Amount}") },
        { "AW", new Template<WithdrawDto>("AW {Account}/{Ip} {Amount}") },
        { "AB", new Template<BalanceDto>("AB {Account}/{Ip}") },
        { "AR", new Template<RemoveAccountDto>("AR {Account}/{Ip}") },
        { "BA", new Template<BankTotalDto>("BA") },
        { "BN", new Template<BankClientsDto>("BN") }
    };
}