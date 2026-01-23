using NetBank.Common.Structures;
using NetBank.Errors;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public class AccountServiceCaptureBuffer : IAccountService, ICaptureBuffer, IReadOnlyStorageCapture
{
    private readonly SequenceReadSet<AccountIdentifier> _touchedAccounts = [];
    private readonly List<TaskCompletionSource<AccountIdentifier>> _creationOperations = [];
    private readonly List<(TaskCompletionSource, AccountIdentifier, Amount)> _depositOperations = [];
    private readonly List<(TaskCompletionSource, AccountIdentifier, Amount)> _withdrawOperations = [];
    private readonly List<(TaskCompletionSource, AccountIdentifier)> _removeOperations = [];
    private readonly List<(TaskCompletionSource<Amount>, AccountIdentifier)> _balanceRequests = [];
    private readonly List<TaskCompletionSource<Amount>> _bankTotalRequests = [];
    private readonly List<TaskCompletionSource<int>> _clientNumberRequests = [];

    public SequenceReadSet<AccountIdentifier> TouchedAccounts => _touchedAccounts;
    public IReadOnlyList<TaskCompletionSource<AccountIdentifier>> CreationOperations => _creationOperations;
    public IReadOnlyList<(TaskCompletionSource, AccountIdentifier, Amount)> DepositOperations => _depositOperations;
    public IReadOnlyList<(TaskCompletionSource, AccountIdentifier, Amount)> WithdrawOperations => _withdrawOperations;
    public IReadOnlyList<(TaskCompletionSource, AccountIdentifier)> RemoveOperations => _removeOperations;
    public IReadOnlyList<(TaskCompletionSource<Amount>, AccountIdentifier)> BalanceRequests => _balanceRequests;
    public IReadOnlyList<TaskCompletionSource<Amount>> BankTotalRequests => _bankTotalRequests;
    public IReadOnlyList<TaskCompletionSource<int>> ClientNumberRequests => _clientNumberRequests;

    public Action? NewClientListener { get; set; }

    public bool HasPending => 
        _creationOperations.Count > 0 ||
        _depositOperations.Count > 0 ||
        _withdrawOperations.Count > 0 ||
        _removeOperations.Count > 0 ||
        _balanceRequests.Count > 0 ||
        _bankTotalRequests.Count > 0 ||
        _clientNumberRequests.Count > 0;

    public void Clear()
    {
        var ex = new ModuleException(
            new ModuleErrorIdentifier(Module.StorageProcessor), 
            ErrorOrigin.System, 
            "Buffer cleared before operations were resolved.");
        
        foreach (var tcs in _creationOperations) tcs.TrySetException(ex);
        foreach (var (tcs, _, _) in _depositOperations) tcs.TrySetException(ex);
        foreach (var (tcs, _, _) in _withdrawOperations) tcs.TrySetException(ex);
        foreach (var (tcs, _) in _removeOperations) tcs.TrySetException(ex);
        foreach (var (tcs, _) in _balanceRequests) tcs.TrySetException(ex);
        foreach (var tcs in _bankTotalRequests) tcs.TrySetException(ex);
        foreach (var tcs in _clientNumberRequests) tcs.TrySetException(ex);

        _touchedAccounts.Clear();
        _creationOperations.Clear();
        _depositOperations.Clear();
        _withdrawOperations.Clear();
        _removeOperations.Clear();
        _balanceRequests.Clear();
        _bankTotalRequests.Clear();
        _clientNumberRequests.Clear();
    }
    
    public Task<AccountIdentifier> CreateAccount() => Deferred<AccountIdentifier>(tcs => 
    {
        _creationOperations.Add(tcs);
    });

    public Task RemoveAccount(AccountIdentifier account) => Deferred(tcs => 
    {
        _touchedAccounts.Add(account);
        _removeOperations.Add((tcs, account));
    });

    public Task Deposit(AccountIdentifier account, Amount amount) => Deferred(tcs => 
    {
        _touchedAccounts.Add(account);
        _depositOperations.Add((tcs, account, amount));
    });

    public Task Withdraw(AccountIdentifier account, Amount amount) => Deferred(tcs => 
    {
        _touchedAccounts.Add(account);
        _withdrawOperations.Add((tcs, account, amount));
    });

    public Task<Amount> Balance(AccountIdentifier account) => Deferred<Amount>(tcs => 
    {
        _touchedAccounts.Add(account);
        _balanceRequests.Add((tcs, account));
    });

    public Task<Amount> BankTotal() => Deferred<Amount>(_bankTotalRequests.Add);

    public Task<int> BankNumberOfClients() => Deferred<int>(_clientNumberRequests.Add);
    
    private Task<T> Deferred<T>(Action<TaskCompletionSource<T>> action)
    {
        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        action(tcs);
        NewClientListener?.Invoke();
        return tcs.Task;
    }

    private Task Deferred(Action<TaskCompletionSource> action)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        action(tcs);
        NewClientListener?.Invoke();
        return tcs.Task;
    }
}