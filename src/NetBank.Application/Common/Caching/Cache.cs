namespace NetBank.Common.Caching;

/// <summary>
/// Generic cache with pluggable eviction policy and configurable capacity.
/// </summary>
public sealed class Cache<TKey, TValue>
{
    private readonly Dictionary<TKey, TValue> storage;
    private readonly ICacheEvictionPolicy<TKey> evictionPolicy;

    private int maximumCapacity;

    public Cache(
        int initialMaximumCapacity,
        ICacheEvictionPolicy<TKey> evictionPolicy)
    {
        if (initialMaximumCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialMaximumCapacity));
        }

        this.evictionPolicy = evictionPolicy;
        maximumCapacity = initialMaximumCapacity;
        storage = new Dictionary<TKey, TValue>(initialMaximumCapacity);
    }

    public Dictionary<TKey, TValue>.KeyCollection Keys => storage.Keys;

    public int MaximumCapacity
    {
        get => maximumCapacity;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            maximumCapacity = value;
            EnforceCapacity();
        }
    }

    public int Count => storage.Count;

    public bool TryGet(TKey key, out TValue value)
    {
        if (storage.TryGetValue(key, out value))
        {
            evictionPolicy.RecordAccess(key);
            return true;
        }

        return false;
    }

    public bool Contains(TKey key)
    {
        return TryGet(key, out _);
    }

    public void Set(TKey key, TValue value)
    {
        if (!storage.ContainsKey(key))
        {
            EnforceCapacityForInsertion();
            evictionPolicy.RecordInsertion(key);
        }

        storage[key] = value;
        evictionPolicy.RecordAccess(key);
    }

    public bool Remove(TKey key)
    {
        if (storage.Remove(key))
        {
            evictionPolicy.RecordRemoval(key);
            return true;
        }

        return false;
    }

    private void EnforceCapacityForInsertion()
    {
        if (storage.Count < maximumCapacity)
        {
            return;
        }

        if (!evictionPolicy.TrySelectEvictionCandidate(out var keyToEvict))
        {
            throw new InvalidOperationException("Eviction policy could not select a key.");
        }

        Remove(keyToEvict);
    }

    private void EnforceCapacity()
    {
        while (storage.Count > maximumCapacity)
        {
            if (!evictionPolicy.TrySelectEvictionCandidate(out var keyToEvict))
            {
                throw new InvalidOperationException("Eviction policy could not select a key.");
            }

            Remove(keyToEvict);
        }
    }

    public void Clear()
    {
        storage.Clear();
    }
}
