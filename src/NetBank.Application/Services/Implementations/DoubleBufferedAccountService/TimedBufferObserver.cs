using NetBank.Common.Structures.Buffering;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public class TimedBufferObserver<T> : IDisposable where T: class, ICaptureBuffer
{
    private readonly Func<Task<bool>> _onShouldSwap;
    private readonly DoubleBuffer<T> _buffer;
    private readonly TimeSpan _swapTimeout;
    
    private CancellationTokenSource? _timerCancellation;
    private readonly object _lock = new();

    public TimedBufferObserver(Func<Task<bool>> onShouldSwap, DoubleBuffer<T> buffer, TimeSpan? swapTimeout = null)
    {
        _onShouldSwap = onShouldSwap;
        _buffer = buffer;
        _swapTimeout = swapTimeout ?? TimeSpan.FromMilliseconds(150);

        _buffer.Front.NewClientListener = OnNewClientDetected;
        _buffer.Back.NewClientListener = OnNewClientDetected;
    }

    private void OnNewClientDetected()
    {
        lock (_lock)
        {
            if (_timerCancellation != null) return;

            _timerCancellation = new CancellationTokenSource();

            _ = StartSwapTimer(_timerCancellation.Token);
        }
    }

    private async Task StartSwapTimer(CancellationToken token)
    {
        bool swapSucceeded = false;

        try
        {
            while (!token.IsCancellationRequested && !swapSucceeded)
            {
                await Task.Delay(_swapTimeout, token);
                
                swapSucceeded = await AttemptSwap();

                /*if (!swapSucceeded)
                {
                    // Optional: Log that we are retrying due to backpressure
                    // The loop will continue and Delay again.
                }*/
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            lock (_lock)
            {
                _timerCancellation?.Dispose();
                _timerCancellation = null;
            
                // Safety: if we exited the loop without success (and not cancelled),
                // and more data arrived in the meantime, re-arm the timer.
                if (!swapSucceeded && !token.IsCancellationRequested && _buffer.Front.HasPending)
                {
                    OnNewClientDetected();
                }
            }
        }
    }
    
    private async Task<bool> AttemptSwap()
    {
        try
        {
            // Change your TrySwap signature to return Task<bool>
            return await _onShouldSwap(); 
        }
        catch
        {
            // If Flush fails, we return false so the timer retries 
            // after another _swapTimeout.
            return false;
        }
    }

    public void Dispose()
    {
        _timerCancellation?.Cancel();
        _timerCancellation?.Dispose();
        
        _buffer.Front.NewClientListener = null;
        _buffer.Back.NewClientListener = null;
    }
}