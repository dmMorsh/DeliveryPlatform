using System.Collections.Concurrent;

namespace Shared.Services;

public sealed class InMemoryEventInbox : IEventInbox
{
    private readonly ConcurrentDictionary<string, string> _events = new();

    public Task<bool> TryStartAsync(
        string eventId,
        string eventType,
        Guid aggregateId,
        string topic,
        int partition,
        long offset,
        CancellationToken ct = default)
    {
        var added = _events.TryAdd(eventId, "processing");
        return Task.FromResult(added);
    }

    public Task MarkProcessedAsync(string eventId, CancellationToken ct = default)
    {
        _events[eventId] = "processed";
        return Task.CompletedTask;
    }

    public Task MarkFailedAsync(string eventId, string error, CancellationToken ct = default)
    {
        _events[eventId] = "failed";
        return Task.CompletedTask;
    }
}
