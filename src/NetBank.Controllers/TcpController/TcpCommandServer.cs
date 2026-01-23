using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NetBank.Errors;

// Assuming your ModuleException is here

namespace NetBank.Controllers.TcpController;

public class TcpCommandServer
{
    private readonly ICommandInterpreter _orchestrator;
    private readonly int _port;
    private readonly IPAddress _ip;
    private readonly TimeSpan _inactivityTimeout;
    private readonly ILogger<TcpCommandServer> _logger;

    public TcpCommandServer(
        ICommandInterpreter orchestrator, 
        int port, 
        IPAddress ip,
        TimeSpan inactivityTimeout,
        ILogger<TcpCommandServer>? logger = null)
    {
        _orchestrator = orchestrator;
        _port = port;
        _inactivityTimeout = inactivityTimeout;
        _logger = logger ?? NullLogger<TcpCommandServer>.Instance;
        _ip = ip;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var listener = new TcpListener(_ip, _port);
        listener.Start();
        
        _logger.LogInformation("TCP Orchestrator started on port {Port}", _port);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient client = await listener.AcceptTcpClientAsync(ct);
                
                _ = HandleClientAsync(client, ct);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Server shutdown initiated via cancellation token.");
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken globalCt)
    {
        var remoteEndPoint = client.Client.RemoteEndPoint;
        
        _logger.LogDebug("Client connected: {RemoteEndPoint}", remoteEndPoint);

        using (client)
        await using (var stream = client.GetStream())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
        await using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
        {
            try
            {
                while (client.Connected && !globalCt.IsCancellationRequested)
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(globalCt);
                    timeoutCts.CancelAfter(_inactivityTimeout);

                    string? command = await reader.ReadLineAsync(timeoutCts.Token);
                    if (string.IsNullOrEmpty(command)) break;

                    string result = await _orchestrator.ExecuteTextCommand(command);
                    await writer.WriteLineAsync(result);
                }
            }
            catch (OperationCanceledException) when (!globalCt.IsCancellationRequested)
            {
                _logger.LogWarning("Client {RemoteEndPoint} disconnected due to inactivity timeout ({Timeout}s).", 
                    remoteEndPoint, _inactivityTimeout.TotalSeconds);
            }
            catch (ModuleException modEx)
            {
                _logger.LogError(modEx, "Module error processing command for {RemoteEndPoint}. ID: {ErrorId}", 
                    remoteEndPoint, modEx.ErrorIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Unexpected crash handling client {RemoteEndPoint}.", remoteEndPoint);
            }
            finally
            {
                _logger.LogDebug("Connection closed: {RemoteEndPoint}", remoteEndPoint);
            }
        }
    }
}