using System.Collections.Concurrent;

namespace Shared.Services;

public sealed class InMemoryEventInbox : IEventInbox
{
    private sealed record EventEntry(string Status, DateTime UpdatedAtUtc);

    private readonly ConcurrentDictionary<string, EventEntry> _events = new();
    private readonly TimeSpan _ttl = TimeSpan.FromHours(1);
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
    private long _lastCleanupTicks = DateTime.UtcNow.Ticks;

    public Task<bool> TryStartAsync(
        string eventId,
        string eventType,
        Guid aggregateId,
        string topic,
        int partition,
        long offset,
        CancellationToken ct = default)
    {
        CleanupIfNeeded();
        var now = DateTime.UtcNow;
        if (_events.TryGetValue(eventId, out var existing))
        {
            if (existing.Status == "failed")
            {
                _events[eventId] = new EventEntry("processing", now);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        var added = _events.TryAdd(eventId, new EventEntry("processing", now));
        return Task.FromResult(added);
    }

    public Task MarkProcessedAsync(string eventId, CancellationToken ct = default)
    {
        CleanupIfNeeded();
        _events[eventId] = new EventEntry("processed", DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(string eventId, string error, CancellationToken ct = default)
    {
        CleanupIfNeeded();
        _events[eventId] = new EventEntry("failed", DateTime.UtcNow);
        return Task.CompletedTask;
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

        foreach (var kvp in _events)
        {
            if (now - kvp.Value.UpdatedAtUtc > _ttl)
                _events.TryRemove(kvp.Key, out _);
        }
    }
}
