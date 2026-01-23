using System.Collections;

namespace NetBank.Common.Structures;

public class SequenceReadSet<T>: IEnumerable<T>
{
    private readonly HashSet<T> _set = new();
    private readonly List<T> _list = new();

    public void Add(T value)
    {
        if (!_set.Add(value)) 
            return;

        _list.Add(value);
    }

    public void Clear()
    {
        _set.Clear();
        _list.Clear();
    }

    public int Count()
    {
        return _list.Count;
    }
    public IEnumerator<T> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}