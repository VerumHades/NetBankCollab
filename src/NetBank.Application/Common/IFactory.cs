namespace NetBank.Common;

public interface IFactory<T>
{
    T Create();
}