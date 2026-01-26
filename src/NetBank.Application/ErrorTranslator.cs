using NetBank.Errors;

namespace NetBank;

public class ErrorTranslator
{
    /// <summary>
    /// Translates a domain error payload into a human-readable English text message.
    /// </summary>
    public static string Translate(object error) => error switch
    {
        InsufficientFundsError e => 
            $"Transaction failed. Account {e.AccountId.Number} has insufficient funds. " +
            $"Current balance: {e.CurrentBalance.Value}, Attempted: {e.AttemptedAmount.Value}.",

        AccountMaxCapacityReachedError => 
            "The bank has reached its maximum account capacity. No new accounts can be created.",

        BufferFlushClearedUnfinishedError => 
            "The request was cancelled because the system buffer was cleared before processing finished.",

        CannotRemoveAccountWithRemainingBalanceError e => 
            $"Cannot close account. A balance of {e.remainingBalance.Value} remains. Please withdraw all funds first.",

        AccountNotFoundError e => 
            $"Access denied. Account {e.AccountId.Number} does not exist in our records.",

        _ => "An unspecified system error occurred."
    };
}