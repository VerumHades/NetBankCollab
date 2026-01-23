using NetBank.Common.Structures.Buffering;

namespace NetBank.Services.Implementations.DoubleBufferedAccountService;

public interface ICaptureObserverFactory<T> where T: ICaptureBuffer
{
    IDisposable Create(DoubleBuffer<T> buffer, Func<Task<bool>> onShouldSwap);
}