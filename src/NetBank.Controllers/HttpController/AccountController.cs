using Microsoft.AspNetCore.Mvc;
using NetBank.Services;

namespace NetBank.Controllers.HttpController;

[Route("api/accounts")]
public class AccountController : HttpControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IStorageStrategy _storageStrategy;
    public AccountController(IAccountService accountService, IStorageStrategy storageStrategy)
    {
        _accountService = accountService;  
        _storageStrategy = storageStrategy;
    }

    [HttpGet("health")]
    public IActionResult Health()
        => Ok(new { status = "OK" });

    [HttpGet("summary")]
    public IActionResult Get()
    {
        return Ok(new
            {
                status = "ok",
                totalAmout = _accountService.BankTotal(),
                numberOfClients = _accountService.BankNumberOfClients()
            }
        );
    }

}