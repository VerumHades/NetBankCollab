using System.Net.WebSockets;
using Microsoft.AspNetCore.Mvc;
using NetBank.Services.Implementations;

namespace NetBank.Controllers.HttpController.controllers;

[Route("ws/tcp-scan")]
public class TcpScanWebSocketController : HttpControllerBase
{
    private readonly NetworkScanService _scanService;

    public TcpScanWebSocketController(NetworkScanService scanService)
    {
        _scanService = scanService;
    }

    /// <summary>
    /// Accepts a WebSocket connection for live scan updates.
    /// Clients receive ScanProgress objects as JSON.
    /// </summary>
    [HttpGet]
    public async Task Get()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = 400;
            return;
        }

        using var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        _scanService.AddWebSocketClient(socket);

        var buffer = new byte[1024 * 4];

        // Keep connection alive until client disconnects
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
            }
        }
    }
}