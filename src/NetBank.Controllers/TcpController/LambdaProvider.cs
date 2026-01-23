using NetBank.Common;

namespace NetBank.Controllers.TcpController;

public class LambdaProvider<T> : IProvider<T>
{
    private readonly Func<T> _factory;

    public LambdaProvider(Func<T> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public T Get()
    {
        return _factory();
    }
}