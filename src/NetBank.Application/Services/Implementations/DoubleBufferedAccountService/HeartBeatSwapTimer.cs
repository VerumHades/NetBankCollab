using Microsoft.Extensions.Logging;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public class HeartbeatSwapTimer : IDisposable
{
    private readonly Func<Task> _swapCallback;
    private readonly TimeSpan _interval;
    private readonly ILogger<HeartbeatSwapTimer>? _logger;
    private readonly object _lock = new();

    private CancellationTokenSource? _cts;
    private Task? _heartbeatTask;
    private bool _isDisposed;

    public HeartbeatSwapTimer(Func<Task> swapCallback, TimeSpan interval, ILogger<HeartbeatSwapTimer>? logger = null)
    {
        _swapCallback = swapCallback;
        _interval = interval;
        _logger = logger;
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_isDisposed || (_heartbeatTask != null && !_heartbeatTask.IsCompleted)) 
                return;

            _cts = new CancellationTokenSource();
            _heartbeatTask = RunHeartbeat(_cts.Token);
            _logger?.LogInformation("Heartbeat timer started.");
        }
    }

    private async Task RunHeartbeat(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, token);
                await _swapCallback();
                
                //_logger?.LogDebug("Heartbeat swap executed successfully.");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Heartbeat swap failed. Retrying next tick.");

                try { await Task.Delay(TimeSpan.FromSeconds(2), token); }
                catch (OperationCanceledException) { break; }
            }
        }

        _logger?.LogInformation("Heartbeat timer stopped.");
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _cts?.Cancel();
            // We don't dispose _cts here because the async loop 
            // might still be referencing it for a microsecond.
            // It will be cleaned up by the GC.
        }
    }
}