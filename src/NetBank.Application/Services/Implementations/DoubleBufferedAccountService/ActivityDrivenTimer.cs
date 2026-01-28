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
    
    private Task? _pulseTask;
    /// <summary>
    /// Wakes the timer. If already pulsing, does nothing.
    /// </summary>

    public void WakeUp()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
        
            if (_pulseTask == null || _pulseTask.IsCompleted)
            {
                _cts = new CancellationTokenSource();
                _pulseTask = PulseLoop(_cts.Token); // Assign the task
                _logger?.LogDebug("Swap timer woke up.");
            }
        }
    }

    private async Task PulseLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, token);

                bool swapped = await _swapCallback();
                if (swapped) return;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "PulseLoop encountered an error.");
                //return;
                await Task.Delay(TimeSpan.FromSeconds(5), token);
            }
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