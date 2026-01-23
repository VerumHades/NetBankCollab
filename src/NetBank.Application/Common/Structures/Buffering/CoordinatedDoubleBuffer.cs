using NetBank.Services.Implementations.DoubleBufferedAccountService;

namespace NetBank.Common.Structures.Buffering;
// Essential for NullLogger

public class CoordinatedDoubleBuffer<T> : DoubleBuffer<T>, IAsyncDisposable where T : ICaptureBuffer
{
    private readonly IProcessor<T> _processor;
    private readonly SemaphoreSlim _swapLock = new(1, 1);
    
    public CoordinatedDoubleBuffer(
        IFactory<T> factory, 
        IProcessor<T> processor): base(factory)
    {
        _processor = processor;
    }

    public async Task<bool> TrySwap()
    {
        if (!await _swapLock.WaitAsync(0)) return false;

        try
        {
            Swap(); 
            await _processor.Flush(Back);
            return true; 
        }
        finally
        {
            _swapLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        try
        {
            await _swapLock.WaitAsync(cts.Token);
        
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
            // Logic reached here because the timeout expired before Flush finished.
            // We log this via the "Edge Logging" pattern we discussed earlier.
            // _logger.LogWarning("Disposal timed out before buffer flush could complete.");
        }
        finally
        {
            _swapLock.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}