using Microsoft.Extensions.Logging;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public class AccountService: IAccountService, IDisposable
{
    private readonly DoubleBufferedAccountCoordinator _coordinator;
    private readonly ILogger<AccountService>? _logger;
    private readonly ActivityDrivenTimer _swapTimer;
    private Action OnActivity { get; }

    
    public AccountService(IStorageStrategy storageStrategy, Configuration.Configuration configuration, ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<AccountService>();
        var processor = new CapturedAccountActionsProcessor(storageStrategy);
        _coordinator = new DoubleBufferedAccountCoordinator(processor);
        
        _swapTimer = new ActivityDrivenTimer(
            () =>
            {
                try
                {
                    return _coordinator.TrySwap();
                }
                catch (Exception e)
                {
                    loggerFactory?.CreateLogger("BufferObserver").LogError(e, "Buffer swap failed.");
                    return Task.FromResult(false);
                }
            }, 
            configuration.BufferSwapDelay,
            loggerFactory?.CreateLogger<ActivityDrivenTimer>()
        );
        
        OnActivity = () =>
        {
            _swapTimer.WakeUp();
        };
    }

    private AccountServiceBufferWriter CurrentWriter
    {
        get
        {
            OnActivity.Invoke();
            return _coordinator.GetWriter();
        }
    }

    public Task<AccountIdentifier> CreateAccount() => CurrentWriter.CreateAccount();
    
    public Task Deposit(AccountIdentifier account, Amount amount) 
        => CurrentWriter.Deposit(account, amount);

    public Task Withdraw(AccountIdentifier account, Amount amount) 
        => CurrentWriter.Withdraw(account, amount);

    public Task RemoveAccount(AccountIdentifier account) 
        => CurrentWriter.RemoveAccount(account);

    public Task<Amount> Balance(AccountIdentifier account) 
        => CurrentWriter.Balance(account);

    public Task<Amount> BankTotal() => CurrentWriter.BankTotal();

    public Task<int> BankNumberOfClients() => CurrentWriter.BankNumberOfClients();

    public void Dispose()
    {
        if (_coordinator is IDisposable coordinatorDisposable)
            coordinatorDisposable.Dispose();
        else
            _ = _coordinator.DisposeAsync().AsTask();
        _swapTimer.Dispose();
    }
}