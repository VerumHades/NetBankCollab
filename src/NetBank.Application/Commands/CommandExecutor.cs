using System.Windows.Input;
using NetBank.Commands.Parsing;
using NetBank.Errors;
using NetBank.Services;

namespace NetBank.Commands;

public class CommandExecutor(IAccountService service, ICommandParser parser, ICommandDelegator delegator, Configuration.Configuration config): ICommandInterpreter
{
    public async Task<string> ExecuteTextCommand(string commandString)
    {
        object? dto = null;
        try
        {
            dto = parser.ParseCommandToDTO(commandString);

            if (dto.GetType() == typeof(WithIpDto) && 
                delegator.ShouldBeDelegated(((WithIpDto)dto).Ip)) 
                return await delegator.DelegateTextCommand(commandString, ((WithIpDto)dto).Ip);
            
            return dto switch
            {
                BankCodeDto =>
                    $"BC {config.ServerIp}",
                
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
        catch (ModuleException ex)
        {
            return $"ER {ErrorTranslator.Translate(ex.ErrorPayload)}";
        }
        catch (Exception ex)
        {
            return $"ER An Unknown Error: {ex.Message}";
        }
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