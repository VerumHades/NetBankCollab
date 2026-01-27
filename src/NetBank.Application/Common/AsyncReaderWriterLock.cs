namespace NetBank.Common;

/// <summary>
/// A lightweight, non-thread-affine Reader-Writer lock for Async/Await.
/// </summary>
public class AsyncReaderWriterLock
{
    private readonly SemaphoreSlim _readerSem = new(1, 1);
    private readonly SemaphoreSlim _writerSem = new(1, 1);
    private int _readerCount = 0;

    public async Task<IDisposable> ReaderLockAsync()
    {
        await _readerSem.WaitAsync();
        if (Interlocked.Increment(ref _readerCount) == 1)
        {
            await _writerSem.WaitAsync(); // First reader blocks writers
        }
        _readerSem.Release();
        return new Releaser(() => ReleaseReader());
    }

    private async Task ReleaseReader()
    {
        await _readerSem.WaitAsync(); // MUST BE ASYNC
        if (Interlocked.Decrement(ref _readerCount) == 0)
        {
            _writerSem.Release(); // Last reader releases writers
        }
        _readerSem.Release();
    }

    public async Task<IDisposable> WriterLockAsync()
    {
        await _writerSem.WaitAsync();
        return new Releaser(() => _writerSem.Release());
    }

    private class Releaser(Action release) : IDisposable 
    {
        public void Dispose() => release();
    }
}