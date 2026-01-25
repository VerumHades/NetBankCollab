using NetBank.Common;
using NetBank.Controllers.TcpController.Parsing;
using NetBank.Services;

namespace NetBank.Controllers.TcpController;

public class CommandExecutor(IProvider<IAccountService> serviceProvider, ICommandParser parser, Configuration.Configuration config): ICommandInterpreter
{
    public async Task<string> ExecuteTextCommand(string commandString)
    {
        var service = serviceProvider.Get();

        object? dto = null;
        try
        {
            dto = parser.ParseCommandToDTO(commandString);
        }
        catch (Exception ex)
        {
            return $"ER {ex}";
        }
        

        return dto switch
        {
            // Identity & Config
            BankCodeDto => 
                $"BC {config.ServerIp}",

            // Account Lifecycle
            CreateAccountDto => 
                $"AC {await service.CreateAccount()}/{config.ServerIp}",

            RemoveAccountDto r => 
                await ExecuteRemove(service, new AccountIdentifier(r.Account)),

            DepositDto r => 
                await ExecuteDeposit(service, new AccountIdentifier(r.Account), new Amount(r.Amount)),

            WithdrawDto r => 
                await ExecuteWithdraw(service, new AccountIdentifier(r.Account), new Amount(r.Amount)),
            
            BalanceDto r => 
                $"AB {await service.Balance(new AccountIdentifier(r.Account))}",

            BankTotalDto => 
                $"BA {await service.BankTotal()}",

            BankClientsDto => 
                $"BN {await service.BankNumberOfClients()}",

            _ => throw new ArgumentException($"Unsupported DTO type: {dto.GetType().Name}")
        };
    }

    private static async Task<string> ExecuteRemove(IAccountService service, AccountIdentifier id)
    {
        await service.RemoveAccount(id);
        return "AR";
    }

    private static async Task<string> ExecuteDeposit(IAccountService service, AccountIdentifier id, Amount amount)
    {
        await service.Deposit(id, amount);
        return "AD";
    }

    private static async Task<string> ExecuteWithdraw(IAccountService service, AccountIdentifier id, Amount amount)
    {
        await service.Withdraw(id, amount);
        return "AW";
    }
}