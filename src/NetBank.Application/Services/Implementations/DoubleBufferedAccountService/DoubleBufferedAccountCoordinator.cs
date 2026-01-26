using NetBank.Common;
using NetBank.Services.Implementations.DoubleBufferedAccountService;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

/// <summary>
/// Orchestrates the rotation and flushing of account service buffers.
/// Decouples memory management from processing logic through composition.
/// </summary>
public class DoubleBufferedAccountCoordinator(IProcessor<AccountServiceCapture> processor) : IAsyncDisposable
{
    private readonly DoubleBuffer<AccountServiceCapture> _buffers = new();
    private readonly SemaphoreSlim _swapLock = new(1, 1);
    
    /// <summary>
    /// Signals that the coordinator is shutting down. 
    /// Linked to the flush processor to abort long-running I/O.
    /// </summary>
    private readonly CancellationTokenSource _lifetimeCts = new();
    private bool _isDisposed;

    /// <summary>
    /// Provides a writer for the currently active front buffer.
    /// </summary>
    public AccountServiceBufferWriter GetWriter()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
        return new AccountServiceBufferWriter(_buffers.Front);
    }

    /// <summary>
    /// Atomically swaps the buffers and initiates a flush of the inactive buffer.
    /// Returns false if a swap is already in progress or the coordinator is disposed.
    /// </summary>
    public async Task<bool> TrySwap()
    {
        try
        {
            // Try to acquire the lock immediately. 
            // If it's held, a flush is already occurring.
            if (!await _swapLock.WaitAsync(0, _lifetimeCts.Token)) 
                return false;
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        try
        {
            // 1. Rotate the buffers
            _buffers.Swap(); 

            // 2. Process the data that was just moved to the back.
            // We pass the lifetime token so the processor can stop if we dispose.
            await processor.Flush(_buffers.Back, _lifetimeCts.Token);
            
            return true; 
        }
        finally
        {
            _swapLock.Release();
        }
    }

    /// <summary>
    /// Gracefully shuts down the coordinator, canceling active flushes and disposing buffers.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // Signal all active flushes to cancel
        await _lifetimeCts.CancelAsync();

        // Wait for the lock to ensure we aren't clearing buffers 
        // while a storage operation is mid-write (within a 5s timeout).
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        try
        {
            await _swapLock.WaitAsync(timeoutCts.Token);
            
            // Dispose the actual capture objects (failing any remaining TCS)
            _buffers.Back?.Dispose();
            _buffers.Front?.Dispose();
        }
        catch (OperationCanceledException)
        {
            // Disposal timed out or was interrupted.
        }
        finally
        {
            _swapLock.Dispose();
            _lifetimeCts.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}