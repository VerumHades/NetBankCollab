using System.Net.Sockets;
using NetBank;
using NetBank.Commands.Parsing;
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
    public async Task Test_BankCode_Command()
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
    
    [Fact]
    public async Task Test_Partial_Withdrawal_Updates_Account_Balance_Correctly()
    {
        var accountId = await CreateAccount();
        const long initialDeposit = 1000;
        const long firstWithdrawal = 300;
        const long secondWithdrawal = 250;

        await Deposit(accountId, initialDeposit);
        await Withdraw(accountId, firstWithdrawal);
        await AssertAccountBalanceEqual(accountId, initialDeposit - firstWithdrawal);
        await Withdraw(accountId, secondWithdrawal);

        long expectedRemaining = initialDeposit - firstWithdrawal - secondWithdrawal;
        await AssertAccountBalanceEqual(accountId, expectedRemaining);
    }
    
    [Fact]
    public async Task Test_Account_ID_Range_Limit_And_Cleanup()
    {
        int startCount = await BankNumberOfClients();

        const int batchSize = 50; 
        var accountIds = new List<int>();

        for (int i = 0; i < batchSize; i++)
        {
            int id = await CreateAccount();

            Assert.InRange(id, 10000, 99999);
            accountIds.Add(id);
        }
        
        Assert.Equal(startCount + batchSize, await BankNumberOfClients());
        var removeTasks = accountIds.Select(id => Remove(id));
        await Task.WhenAll(removeTasks);

        
        int finalCount = await BankNumberOfClients();
        Assert.Equal(startCount, finalCount);
    }
    
    private async Task<string> Remove(int accountId)
    {
        var command = GetTemplate<RemoveAccountDto>("AR").Construct(new RemoveAccountDto(accountId, _fixture.Address));
        return await SendCommand(command);
    }
    
    [Fact]
    public async Task Test_Remove_NonExistent_Errors()
    {
        var response = await Remove(0);
        Assert.StartsWith("ER Access denied. Account", response);
    }
    
    [Fact]
    public async Task Test_Remove_Existent()
    {
        var accountId = await CreateAccount();
        var response = await Remove(accountId);
        Assert.Equal("AR", response);
    }
    
    class BankAmountResponseDto
    {
        public long Amount { get; set; }
    }

    private async Task<long> BankTotal()
    {
        var response = await SendCommand("BA");
        return new Template<BankAmountResponseDto>("BA {Amount}").Parse(response).Amount;
    }
    
    [Fact]
    public async Task Test_BankAmount_Increments_On_Deposit()
    {
        const int accountCount = 10;
        const long addition = 956;

        var beforeAmount = await BankTotal();

        for (int i = 0; i < accountCount; i++)
        {
            var accountId = await CreateAccount();
            await Deposit(accountId, addition);
        }
        
        var expectedAmount = beforeAmount + addition * accountCount;
        var afterAmount = await BankTotal();
        
        Assert.Equal(expectedAmount, afterAmount);
    }
    
    [Fact]
    public async Task Test_BankAmount_Decrements_On_Withdrawal()
    {
        var initialBankTotal = await BankTotal();
        var accountId = await CreateAccount();
        const long depositAmount = 5000;
        const long withdrawAmount = 2000;

        await Deposit(accountId, depositAmount);

        Assert.Equal(initialBankTotal + depositAmount, await BankTotal());

        var response = await Withdraw(accountId, withdrawAmount);
        Assert.Equal("AW", response);
        
        var expectedTotal = initialBankTotal + (depositAmount - withdrawAmount);
        Assert.Equal(expectedTotal, await BankTotal());
    }
    
    class BankNumberOfClientsDto
    {
        public int Clients { get; set; }
    }

    private async Task<int> BankNumberOfClients()
    {
        var response = await SendCommand("BN");
        return new Template<BankNumberOfClientsDto>("BN {Clients}").Parse(response).Clients;
    }
    
    [Fact]
    public async Task Test_Bank_Number_Of_Clients_Increments_Correctly()
    {
        int initialCount = await BankNumberOfClients();

        const int newAccountsToCreate = 5;
        var creationTasks = Enumerable.Range(0, newAccountsToCreate)
            .Select(_ => CreateAccount());
    
        await Task.WhenAll(creationTasks);

        int finalCount = await BankNumberOfClients();
    
        Assert.Equal(initialCount + newAccountsToCreate, finalCount);
    }
    
    [Fact]
    public async Task Test_Bank_Number_Of_Clients_Decrements_Correctly()
    {
        var initialClients = await BankNumberOfClients();
        
        var id1 = await CreateAccount();
        var id2 = await CreateAccount();
        
        Assert.Equal(initialClients + 2, await BankNumberOfClients());
        await Remove(id1);

        var finalClients = await BankNumberOfClients();
        Assert.Equal(initialClients + 1, finalClients);
    }
    
    [Fact]
    public async Task Test_Continuous_Requests_Through_Multiple_Swaps()
    {
        int requestIntervalMs = 5;
        int testDurationMs = 1000;

        var accountIds = new List<int>();
        var startTime = DateTime.UtcNow;
        
        while ((DateTime.UtcNow - startTime).TotalMilliseconds < testDurationMs)
        {
            var id = await CreateAccount();
            accountIds.Add(id);
        
            await Task.Delay(requestIntervalMs);
        }
        
        Assert.NotEmpty(accountIds);

        var uniqueIds = accountIds.Distinct().Count();
        Assert.Equal(accountIds.Count, uniqueIds);
    }
    
    [Fact]
    public async Task Test_E2E_Concurrency_During_Active_Swapping()
    {
        const int totalClients = 100; 
        const long depositPerClient = 100;
        const int delayBetweenRequestsMs = 5; 
        
        var initialBankTotalAmount = await BankTotal();
        
        var tasks = new List<Task>();
        
        for (int i = 0; i < totalClients; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(Random.Shared.Next(0, 100));
                
                var accountId = await CreateAccount();
                
                await Deposit(accountId, depositPerClient / 2);
                await Task.Delay(delayBetweenRequestsMs);
                await Deposit(accountId, depositPerClient / 2);
            }));
        }
        
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(20));
        var completedTask = await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException("The E2E test hung! This points to a deadlock in the Proxy or Server.");
        }
        
        var actualTotalAmount = await BankTotal();
        var expectedTotalAmount = totalClients * depositPerClient + initialBankTotalAmount;

        Assert.Equal(expectedTotalAmount, actualTotalAmount);
    }
}