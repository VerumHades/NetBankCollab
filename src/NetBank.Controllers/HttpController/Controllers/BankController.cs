using Microsoft.AspNetCore.Mvc;
using NetBank.Errors;
using NetBank.Services;

namespace NetBank.Controllers.HttpController.controllers;


/// <summary>
/// Provides HTTP endpoints for account-related diagnostics
/// and aggregated bank information.
/// </summary>
[Route("api/bank")]
public class BankController : HttpControllerBase
{
    private readonly IAccountService _accountService;
    public BankController(IAccountService accountService)
    {
        _accountService = accountService;  
    }
    // --------------------
    // Bank aggregates
    // --------------------

    /// <summary>
    /// Retrieves bank-wide summary information.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<object>> GetBankSummary()
    {
        var total = await _accountService.BankTotal();
        var clients = await _accountService.BankNumberOfClients();

        return Ok(new
        {
            totalAmount = total,
            numberOfClients = clients
        });
    }

}