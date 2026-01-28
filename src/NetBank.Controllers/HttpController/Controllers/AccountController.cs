using Microsoft.AspNetCore.Mvc;
using NetBank.Services;

namespace NetBank.Controllers.HttpController.controllers;

[Route("api/accounts")]
public class AccountController : HttpControllerBase
{
    private readonly IAccountService _accountService;

    public AccountController(IAccountService accountService)
    {
        _accountService = accountService;
    }
    
    /// <summary>
    /// Creates a new bank account.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AccountIdentifier>> CreateAccount()
    {
        var accountIdentifier = await _accountService.CreateAccount();
        var amount = await  _accountService.Balance(accountIdentifier);
        return CreatedAtAction(nameof(GetBalance), new Account(accountIdentifier,amount  ) );
    }

    /// <summary>
    /// Deletes an existing bank account.
    /// </summary>
    [HttpDelete("{accountId}")]
    public async Task<IActionResult> DeleteAccount([FromRoute] AccountIdentifier accountId)
    {
        await _accountService.RemoveAccount(accountId);
        return NoContent();
    }

    /// <summary>
    /// Deposits an amount into the specified account.
    /// </summary>
    [HttpPost("{accountId}/deposit")]
    public async Task<IActionResult> Deposit(
        [FromRoute] AccountIdentifier accountId,
        [FromBody] Amount amount)
    {
        await _accountService.Deposit(accountId, amount);
        return NoContent();
    }

    /// <summary>
    /// Withdraws an amount from the specified account.
    /// </summary>
    [HttpPost("{accountId}/withdraw")]
    public async Task<IActionResult> Withdraw(
        [FromRoute] AccountIdentifier accountId,
        [FromBody] Amount amount)
    {
        await _accountService.Withdraw(accountId, amount);
        return NoContent();
    }

    /// <summary>
    /// Returns the current balance of the specified account.
    /// </summary>
    [HttpGet("{accountId}/balance")]
    public async Task<ActionResult<Amount>> GetBalance([FromRoute] AccountIdentifier accountId)
    {
        var balance = await _accountService.Balance(accountId);
        return Ok(balance);
    }
}