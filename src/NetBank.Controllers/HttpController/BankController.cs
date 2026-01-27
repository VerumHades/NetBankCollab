using Microsoft.AspNetCore.Mvc;
using NetBank.Errors;
using NetBank.Services;

namespace NetBank.Controllers.HttpController;


/// <summary>
/// Provides HTTP endpoints for account-related diagnostics
/// and aggregated bank information.
/// </summary>
[ApiController]
[Route("api/accounts")]
public class BankController : HttpControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IStorageStrategy _storage;
    public BankController(IAccountService accountService, IStorageStrategy storageStrategy)
    {
        _accountService = accountService;  
        _storage = storageStrategy;
    }
   /// <summary>
    /// Creates new bank accounts with initial balances.
    /// </summary>
    [HttpPost("accounts")]
    public async Task<ActionResult<IReadOnlyList<AccountIdentifier>>> CreateAccounts(
        [FromBody] IEnumerable<Account> accounts)
    {
        var created = await _storage.UpdateAll(accounts);
        return Created("api/accounts", created);
    }

    /// <summary>
    /// Updates account balances
    /// </summary>
    [HttpPatch("accounts")]
    public async Task<ActionResult<IReadOnlyList<AccountIdentifier>>> UpdateAccounts(
        [FromBody] IEnumerable<Account> updates)
    {
        try
        {
            var updated = await _storage.UpdateAll(updates);
            return Ok(updated);
        }
        catch (Exception exception) 
        {
            return BadRequest(new
            {
                error = exception.GetType(),
                message = exception.Message,
            });
        }
    }

    /// <summary>
    /// Deletes the specified accounts.
    /// </summary>
    [HttpDelete("accounts")]
    public async Task<ActionResult<IReadOnlyList<AccountIdentifier>>> DeleteAccounts(
        [FromBody] IEnumerable<AccountIdentifier> accounts)
    {
        var removed = await _storage.RemoveAccounts(accounts);
        return Ok(removed);
    }

    /// <summary>
    /// Retrieves balances for the specified accounts.
    /// </summary>
    [HttpGet("accounts")]
    public async Task<ActionResult<IReadOnlyList<Account>>> GetAccounts(
        [FromQuery] IEnumerable<AccountIdentifier> ids)
    {
        var result = await _storage.GetAll(ids);
        return Ok(result);
    }

    // --------------------
    // Bank aggregates
    // --------------------

    /// <summary>
    /// Retrieves bank-wide summary information.
    /// </summary>
    [HttpGet("bank")]
    public async Task<ActionResult<object>> GetBankSummary()
    {
        var total = await _storage.BankTotal();
        var clients = await _storage.BankNumberOfClients();

        return Ok(new
        {
            totalAmount = total,
            numberOfClients = clients
        });
    }

}