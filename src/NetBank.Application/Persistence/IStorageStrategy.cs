namespace NetBank.Persistence;

/// <summary>
/// Defines the storage strategy for accounts and balances.
/// </summary>
public interface IStorageStrategy
{
    /// <summary>
    /// Creates a number of accounts. The actual number of accounts created
    /// may not match the requested count.
    /// </summary>
    /// <param name="count">The desired number of accounts to create.</param>
    /// <returns>A collection of identifiers for the created accounts.</returns>
    Task<IReadOnlyList<AccountIdentifier>> CreateAccounts(int count);

    /// <summary>
    /// Deletes the specified accounts. Accounts that do not exist are ignored.
    /// </summary>
    /// <param name="accounts">The accounts to delete.</param>
    /// <returns>A collection of identifiers of accounts that were removed.</returns>
    Task<IReadOnlyList<AccountIdentifier>> RemoveAccounts(IEnumerable<AccountIdentifier> accounts);

    /// <summary>
    /// Deposits amounts into the specified accounts. Correctness is not guaranteed.
    /// </summary>
    /// <param name="amounts">The accounts and amounts to deposit.</param>
    /// <returns>A collection of identifiers of accounts that were updated.</returns>
    Task<IReadOnlyList<AccountIdentifier>> DepositAll(IEnumerable<AccountAndAmount> amounts);

    /// <summary>
    /// Withdraws amounts from the specified accounts. Correctness is not guaranteed.
    /// </summary>
    /// <param name="amounts">The accounts and amounts to withdraw.</param>
    /// <returns>A collection of identifiers of accounts that were updated.</returns>
    Task<IReadOnlyList<AccountIdentifier>> WithdrawAll(IEnumerable<AccountAndAmount> amounts);

    /// <summary>
    /// Retrieves balances for the specified accounts.
    /// Accounts that do not exist are omitted from the results.
    /// </summary>
    /// <param name="accounts">The accounts to query.</param>
    /// <returns>A collection of accounts with their balances.</returns>
    Task<IReadOnlyList<AccountAndAmount>> BalanceAll(IEnumerable<AccountIdentifier> accounts);

    /// <summary>
    /// Returns the total sum of all amounts across all accounts.
    /// </summary>
    /// <returns>The total bank amount.</returns>
    Task<Amount> BankTotal();

    /// <summary>
    /// Returns the number of existing accounts.
    /// </summary>
    /// <returns>The total number of accounts.</returns>
    Task<int> BankNumberOfClients();
}
