using NetBank.Services.Implementations.DoubleBufferedAccountService;

namespace NetBank.Common.Structures.Buffering;

/// <summary>
/// A double buffer that automatically flushes the inactive buffer to a processor upon swapping.
/// Supports graceful cancellation and disposal.
/// </summary>
public class FlushOnSwapDoubleBuffer<T> : DoubleBuffer<T>, IAsyncDisposable where T : ICaptureBuffer
{
    private readonly IProcessor<T> _processor;
    private readonly SemaphoreSlim _swapLock = new(1, 1);
    
    /// <summary>
    /// Internal CTS to signal that the buffer is disposing and all active work should stop.
    /// </summary>
    private readonly CancellationTokenSource _lifetimeCts = new();

    public FlushOnSwapDoubleBuffer(
        IFactory<T> factory, 
        IProcessor<T> processor): base(factory)
    {
        _processor = processor;
    }

    /// <summary>
    /// Attempts to swap the buffers and flush the back buffer. 
    /// Returns false if a swap is already in progress or if the buffer is disposed.
    /// </summary>
    public async Task<bool> TrySwap()
    {
        try
        {
            // If we can't get the lock immediately, another swap is running.
            // We pass _lifetimeCts.Token so that if Dispose is called, this wait exits.
            if (!await _swapLock.WaitAsync(0, _lifetimeCts.Token)) return false;
        }
        catch (OperationCanceledException)
        {
            return false; // Disposal initiated
        }

        try
        {
            Swap(); 
            // We pass the lifetime token to the processor. 
            // If DisposeAsync is called while Flushing, the processor can abort.
            await _processor.Flush(Back, _lifetimeCts.Token);
            return true; 
        }
        finally
        {
            _swapLock.Release();
        }
    }

    /// <summary>
    /// Gracefully shuts down the buffer, canceling active flushes and clearing memory.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _lifetimeCts.CancelAsync();

        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            await _swapLock.WaitAsync(timeoutCts.Token);
        
            try
            {
                Back?.Clear();
                Front?.Clear();
            }
            finally
            {
                _swapLock.Release();
            }
        }
        catch (OperationCanceledException)
        {
            // Log: "Disposal timed out or was interrupted."
        }
        finally
        {
            _swapLock.Dispose();
            _lifetimeCts.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}