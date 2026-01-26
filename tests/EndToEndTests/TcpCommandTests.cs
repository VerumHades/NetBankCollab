using System.Net.Sockets;
using NetBank;
using NetBank.Controllers.TcpController.Parsing;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace EndToEndTests;

public class TcpCommandTests : IClassFixture<BankServerFixture>
{
    private readonly BankServerFixture _fixture;

    public TcpCommandTests(BankServerFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<string> SendCommand(string command)
    {
        using var client = new TcpClient(_fixture.Address, _fixture.Port);
        using var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
        using var reader = new StreamReader(client.GetStream());

        await writer.WriteLineAsync(command);
        return await reader.ReadLineAsync() ?? "";
    }
    
    private Template<T> GetTemplate<T>(string name) where T : new()
    {
        return (Template<T>)ProtocolRegistry.Templates[name];
    }
    
    [Fact]
    public async Task Test_Broadcast_Command()
    {
        var response = await SendCommand("BC");
        Assert.Equal($"BC {_fixture.Address}", response);
    }
    
    class AccountCreateResponseDto
    {
        public int Account { get; set; }
        public string Ip { get; set; }
    }
    
    private async Task<int> CreateAccount()
    {
        var response = await SendCommand("AC");
        Assert.StartsWith("AC ", response);
        var dto = new Template<AccountCreateResponseDto>("AC {Account}/{Ip}").Parse(response);
        Assert.Equal(_fixture.Address, dto.Ip);
        return dto.Account;
    }
    
    [Fact]
    public async Task Test_Create_Account()
    {
        await CreateAccount();
    }
    
    class AccountBalanceResponseDto
    {
        public long Amount { get; set; }
    }
    
    private async Task<long> GetAccountBalance(int id)
    {
        var command = GetTemplate<BalanceDto>("AB").Construct(new BalanceDto(id, _fixture.Address));
        var response = await SendCommand(command);
        Assert.StartsWith("AB ", response);
        var dto = new Template<AccountBalanceResponseDto>("AB {Amount}").Parse(response);
        return dto.Amount;
    }

    private async Task AssertAccountBalanceEqual(int accountId, long amount)
    {
        var balance = await GetAccountBalance(accountId);
        Assert.Equal(amount, balance);
    }

    [Fact]
    public async Task Test_Account_Balance()
    {
        var accountId = await CreateAccount();
        var balance = await GetAccountBalance(accountId);
        Assert.Equal(0, balance);
    }

    private async Task Deposit(int accountId, long amount)
    {
        var command = GetTemplate<DepositDto>("AD").Construct(new DepositDto(accountId, _fixture.Address, amount));
        await SendCommand(command);
    }
    
    [Fact]
    public async Task Test_Deposit_Account()
    {
        var accountId = await CreateAccount();
        await Deposit(accountId, 1000);
        await AssertAccountBalanceEqual(accountId, 1000);
    }

    private async Task<string> Withdraw(int accountId, long amount)
    {
        var command = GetTemplate<WithdrawDto>("AW").Construct(new WithdrawDto(accountId, _fixture.Address, amount));
        return await SendCommand(command);
    }
    
    [Fact]
    public async Task Test_Withdraw_Account()
    {
        var accountId = await CreateAccount();
        await Deposit(accountId, 1000);
        await AssertAccountBalanceEqual(accountId, 1000);
        await Withdraw(accountId, 1000);
        await AssertAccountBalanceEqual(accountId, 0);
    }
    
    [Fact]
    public async Task Test_Withdraw_TooMuch()
    {
        var accountId = await CreateAccount();
        var response = await Withdraw(accountId, 1000);
        Assert.StartsWith("ER Transaction failed. ", response);
        
        await AssertAccountBalanceEqual(accountId, 0);
    }
}