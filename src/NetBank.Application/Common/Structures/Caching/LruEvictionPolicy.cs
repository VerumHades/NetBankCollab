namespace NetBank.Common.Structures.Caching;

/// <summary>
/// Least-recently-used eviction policy.
/// </summary>
/// <typeparam name="TKey">The cache key type.</typeparam>
public sealed class LruEvictionPolicy<TKey> : ICacheEvictionPolicy<TKey>
{
    private readonly LinkedList<TKey> usageOrder = new();
    private readonly Dictionary<TKey, LinkedListNode<TKey>> nodeIndex = new();

    public void RecordAccess(TKey key)
    {
        if (!nodeIndex.TryGetValue(key, out var node))
        {
            return;
        }

        usageOrder.Remove(node);
        usageOrder.AddLast(node);
    }

    public void RecordInsertion(TKey key)
    {
        var node = new LinkedListNode<TKey>(key);
        nodeIndex[key] = node;
        usageOrder.AddLast(node);
    }

    public void RecordRemoval(TKey key)
    {
        if (!nodeIndex.TryGetValue(key, out var node))
        {
            return;
        }

        usageOrder.Remove(node);
        nodeIndex.Remove(key);
    }

    public bool TrySelectEvictionCandidate(out TKey key)
    {
        if (usageOrder.First is null)
        {
            key = default!;
            return false;
        }

        key = usageOrder.First.Value;
        return true;
    }

    public void Clear()
    {
        usageOrder.Clear();
        nodeIndex.Clear();
    }
}
