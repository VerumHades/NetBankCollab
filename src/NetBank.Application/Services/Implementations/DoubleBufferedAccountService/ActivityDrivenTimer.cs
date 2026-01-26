using Microsoft.Extensions.Logging;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public class ActivityDrivenTimer : IDisposable
{
    private readonly Func<Task<bool>> _swapCallback;
    private readonly TimeSpan _interval;
    private readonly object _lock = new();
    private readonly ILogger<ActivityDrivenTimer>? _logger;
    
    private CancellationTokenSource? _cts;
    private bool _isDisposed;

    /// <param name="swapCallback">Function to execute. Should return TRUE if it wants to pulse again, FALSE to sleep.</param>
    /// <param name="interval">Time between pulses.</param>
    /// <param name="logger"></param>
    public ActivityDrivenTimer(Func<Task<bool>> swapCallback, TimeSpan interval, ILogger<ActivityDrivenTimer>? logger = null)
    {
        _swapCallback = swapCallback;
        _interval = interval;
        _logger = logger;
    }

    /// <summary>
    /// Wakes the timer. If already pulsing, does nothing.
    /// </summary>
    public void WakeUp()
    {
        lock (_lock)
        {
            if (_isDisposed || _cts != null) return;

            _cts = new CancellationTokenSource();
            _ = PulseLoop(_cts.Token);
            _logger?.LogDebug("Swap timer woke up.");
        }
    }

    private async Task PulseLoop(CancellationToken token)
    {
        try
        {
            bool keepPulsing = true;

            while (keepPulsing && !token.IsCancellationRequested)
            {
                await Task.Delay(_interval, token);
                
                keepPulsing = !await _swapCallback();
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            lock (_lock)
            {
                _cts?.Dispose();
                _cts = null;
            }
            
            _logger?.LogDebug("Swap went to sleep.");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _isDisposed = true;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}