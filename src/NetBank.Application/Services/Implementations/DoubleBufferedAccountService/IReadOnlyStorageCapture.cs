using NetBank.Common.Structures;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public interface IReadOnlyStorageCapture
{
    SequenceReadSet<AccountIdentifier> TouchedAccounts { get; }

    IReadOnlyList<TaskCompletionSource<AccountIdentifier>> CreationOperations { get; }
    IReadOnlyList<(TaskCompletionSource, AccountIdentifier, Amount)> DepositOperations { get; }
    IReadOnlyList<(TaskCompletionSource, AccountIdentifier, Amount)> WithdrawOperations { get; }
    IReadOnlyList<(TaskCompletionSource, AccountIdentifier)> RemoveOperations { get; }
    IReadOnlyList<(TaskCompletionSource<Amount>, AccountIdentifier)> BalanceRequests { get; }

    IReadOnlyList<TaskCompletionSource<Amount>> BankTotalRequests { get; }
    IReadOnlyList<TaskCompletionSource<int>> ClientNumberRequests { get; }
}