namespace NetBank.Errors;


// This error is thrown when trying to withdraw from a domain account entity that has insufficent funds for the withdraw
public record InsufficientFundsError(AccountIdentifier AccountId, Amount AttemptedAmount, Amount CurrentBalance);

// This error is returned to tasks asking to create an account but there is no more space for accounts
public record AccountMaxCapacityReachedError();

// This error is throw for each task if the buffer clears before the request could be resolved
public record BufferFlushClearedUnfinishedError();

// This error is thrown for requests that try to remove an account that has remaining balance (i.e. balance != 0)
public record CannotRemoveAccountWithRemainingBalanceError(Amount remainingBalance);

// This error is thrown for requests regarding an account that does not exist
public record AccountNotFoundError(AccountIdentifier AccountId);