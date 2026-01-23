namespace NetBank.Common;

public interface IProvider<T>
{
    T Get();
}