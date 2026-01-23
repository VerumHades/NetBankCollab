namespace NetBank.Common;

public interface IProcessor<T>
{
    Task Flush(T value);
}