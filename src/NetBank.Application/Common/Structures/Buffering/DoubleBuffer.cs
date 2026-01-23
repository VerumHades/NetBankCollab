namespace NetBank.Common.Structures.Buffering;

public class DoubleBuffer<T>
{
    public T Front => _buffers[_front];

    public T Back => _buffers[NextBuffer()];
    
    private readonly T[] _buffers;
    private int _front = 0;
    
    public DoubleBuffer(IFactory<T> factory)
    {
        _buffers = new[]
        {
            factory.Create(),
            factory.Create()
        };
    }

    private int NextBuffer()
    {
        return (_front + 1) % _buffers.Length;
    }

    public void Swap()
    {
        _front = NextBuffer();
    }
}