using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Thread-Safe LRU Cache with TTL per entry.
/// Key features:
/// - Evicts least-recently-used item when capacity exceeds.
/// - Each entry expires after a given TTL.
/// - Safe for concurrent Get/Put operations.
/// </summary>
public class LRUCache<TKey, TValue>
{
    private readonly int _capacity; // Maximum number of cache entries
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _map; // Key lookup
    private readonly LinkedList<CacheItem> _lruList; // Tracks recency (head = most recent)
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(); // Thread safety

    // Internal cache item
    private class CacheItem
    {
        public TKey Key;
        public TValue Value;
        public DateTime Expiration; // TTL
    }

    public LRUCache(int capacity)
    {
        _capacity = capacity;
        _map = new Dictionary<TKey, LinkedListNode<CacheItem>>();
        _lruList = new LinkedList<CacheItem>();
    }

    /// <summary>
    /// Get value by key. Returns default(TValue) if not found or expired.
    /// Moves accessed node to front (most recently used).
    /// </summary>
    public TValue Get(TKey key)
    {
        _lock.EnterUpgradeableReadLock(); // Allows upgrade to write if needed
        try
        {
            if (!_map.TryGetValue(key, out var node))
                return default(TValue); // Key not found

            // Check TTL
            if (node.Value.Expiration < DateTime.UtcNow)
            {
                Remove(key); // Remove expired item
                return default(TValue);
            }

            // Move node to head (most recently used)
            _lock.EnterWriteLock();
            try
            {
                _lruList.Remove(node);
                _lruList.AddFirst(node);
            }
            finally { _lock.ExitWriteLock(); }

            return node.Value.Value;
        }
        finally { _lock.ExitUpgradeableReadLock(); }
    }

    /// <summary>
    /// Insert or update value with TTL.
    /// Evicts least-recently-used item if capacity exceeded.
    /// </summary>
    public void Put(TKey key, TValue value, TimeSpan ttl)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_map.TryGetValue(key, out var existing))
            {
                // Remove old node if it exists
                _lruList.Remove(existing);
            }

            var item = new CacheItem
            {
                Key = key,
                Value = value,
                Expiration = DateTime.UtcNow.Add(ttl)
            };

            // Insert at head (most recently used)
            var node = new LinkedListNode<CacheItem>(item);
            _lruList.AddFirst(node);
            _map[key] = node;

            // Evict least recently used if over capacity
            if (_map.Count > _capacity)
            {
                var last = _lruList.Last;
                _lruList.RemoveLast();
                _map.Remove(last.Value.Key);
            }
        }
        finally { _lock.ExitWriteLock(); }
    }

    // Helper to remove key
    private void Remove(TKey key)
    {
        if (_map.TryGetValue(key, out var node))
        {
            _lruList.Remove(node);
            _map.Remove(key);
        }
    }
}

// ===== TEST =====
class Program
{
    static void Main()
    {
        var cache = new LRUCache<int, string>(2);

        cache.Put(1, "A", TimeSpan.FromSeconds(5)); // Insert key 1
        cache.Put(2, "B", TimeSpan.FromSeconds(5)); // Insert key 2

        Console.WriteLine(cache.Get(1)); // Access key 1 → moves to head, prints A

        cache.Put(3, "C", TimeSpan.FromSeconds(5)); // Insert key 3 → evicts least recently used (key 2)

        Console.WriteLine(cache.Get(2)); // Key 2 evicted → prints null

        Thread.Sleep(6000); // Wait for TTL to expire
        Console.WriteLine(cache.Get(1)); // Key 1 expired → prints null
    }
}