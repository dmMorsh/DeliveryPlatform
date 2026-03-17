using System.Collections.Concurrent;

namespace Shared.Services;

public sealed class MemoryTtlCache<TKey, TValue> where TKey : notnull
{
    private sealed record CacheEntry(TValue Value, DateTime ExpiresAt);

    private readonly ConcurrentDictionary<TKey, CacheEntry> _entries = new();
    private readonly TimeSpan _cleanupInterval;
    private long _lastCleanupTicks = DateTime.UtcNow.Ticks;

    public MemoryTtlCache(TimeSpan? cleanupInterval = null)
    {
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(1);
    }

    public bool TryGet(TKey key, out TValue value)
    {
        value = default!;

        CleanupIfNeeded();
        if (!_entries.TryGetValue(key, out var entry))
            return false;

        if (entry.ExpiresAt <= DateTime.UtcNow)
        {
            _entries.TryRemove(key, out _);
            return false;
        }

        value = entry.Value;
        return true;
    }

    public void Set(TKey key, TValue value, TimeSpan ttl)
    {
        CleanupIfNeeded();
        var expiresAt = DateTime.UtcNow.Add(ttl);
        _entries[key] = new CacheEntry(value, expiresAt);
    }

    public void Remove(TKey key)
    {
        CleanupIfNeeded();
        _entries.TryRemove(key, out _);
    }

    private void CleanupIfNeeded()
    {
        var now = DateTime.UtcNow;
        var lastTicks = Interlocked.Read(ref _lastCleanupTicks);
        var last = new DateTime(lastTicks, DateTimeKind.Utc);
        if (now - last < _cleanupInterval)
            return;

        if (Interlocked.CompareExchange(ref _lastCleanupTicks, now.Ticks, lastTicks) != lastTicks)
            return;

        foreach (var kvp in _entries)
        {
            if (kvp.Value.ExpiresAt <= now)
                _entries.TryRemove(kvp.Key, out _);
        }
    }
}
