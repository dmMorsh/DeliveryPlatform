namespace Shared.Services;

public interface IEventInbox
{
    Task<bool> TryStartAsync(
        string eventId,
        string eventType,
        Guid aggregateId,
        string topic,
        int partition,
        long offset,
        CancellationToken ct = default);

    Task MarkProcessedAsync(string eventId, CancellationToken ct = default);
    Task MarkFailedAsync(string eventId, string error, CancellationToken ct = default);
}
