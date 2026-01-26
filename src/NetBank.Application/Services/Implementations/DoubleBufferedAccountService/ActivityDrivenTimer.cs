namespace NetBank.Common.Structures.Buffering;

public class ActivityDrivenTimer : IDisposable
{
    private readonly Func<Task<bool>> _swapCallback;
    private readonly TimeSpan _interval;
    private readonly object _lock = new();
    
    private CancellationTokenSource? _cts;
    private bool _isDisposed;

    /// <param name="swapCallback">Function to execute. Should return TRUE if it wants to pulse again, FALSE to sleep.</param>
    /// <param name="interval">Time between pulses.</param>
    public ActivityDrivenTimer(Func<Task<bool>> swapCallback, TimeSpan interval)
    {
        _swapCallback = swapCallback;
        _interval = interval;
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
        }
    }

    private async Task PulseLoop(CancellationToken token)
    {
        try
        {
            bool keepPulsing = true;

            while (keepPulsing && !token.IsCancellationRequested)
            {
                // Wait first to batch the initial burst of calls
                await Task.Delay(_interval, token);

                // Execute the swap and ask: "Should I stay awake?"
                keepPulsing = await _swapCallback();
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