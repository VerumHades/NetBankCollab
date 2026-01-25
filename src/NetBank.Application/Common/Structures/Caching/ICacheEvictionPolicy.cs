namespace NetBank.Common.Structures.Caching;

/// <summary>
/// Defines an eviction policy for a cache.
/// </summary>
/// <typeparam name="TKey">The cache key type.</typeparam>
public interface ICacheEvictionPolicy<TKey>
{
    void RecordAccess(TKey key);

    void RecordInsertion(TKey key);

    void RecordRemoval(TKey key);

    bool TrySelectEvictionCandidate(out TKey key);

    void Clear();
}
