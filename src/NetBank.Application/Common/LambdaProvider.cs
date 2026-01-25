namespace NetBank.Common;

/// <summary>
/// A provider that resolves a dependency using a provided delegate.
/// </summary>
public class LambdaProvider<T>(Func<T> resolver) : IProvider<T>
{
    public T Get() => resolver();
}