using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using NetBank.NetworkScan;
using NetBank.Services;
using NetBank.Services.Implementations;
using NetBank.Services.NetworkScan;

namespace NetBank.Controllers.HttpController.controllers;

[Route("api/tcp-scan")]
public class TcpScanController: HttpControllerBase
{
    private readonly NetworkScanService _scanService;
    

    public TcpScanController(NetworkScanService scanService)
    {
        _scanService = scanService;
    }
    /// <summary>
    /// Starts a TCP scan on the local network.
    /// Progress is sent to the client using websocket
    /// </summary>
    [HttpPost("start")]
    public IActionResult StartScan([FromBody] ScanRequest request)
    {
        _ = Task.Run(() => _scanService.StartScanAsync(request));

        return Ok(new { status = "scan_started" });
    }
}