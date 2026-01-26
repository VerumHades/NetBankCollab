namespace NetBank.Common.Structures.Buffering;

public class DoubleBuffer<T> where T: new()
{
    public T Front => _buffers[_front];

    public T Back => _buffers[NextBuffer()];
    
    private readonly T[] _buffers = [new(), new()];
    private int _front;
    
    private int NextBuffer()
    {
        return (_front + 1) % _buffers.Length;
    }

    public void Swap()
    {
        _front = NextBuffer();
    }
}