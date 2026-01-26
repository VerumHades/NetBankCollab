using Microsoft.AspNetCore.Mvc;

namespace NetBank.Controllers.HttpController;

[Route("api/accounts")]
public class AccountController : HttpControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
        => Ok(new { status = "OK" });
}