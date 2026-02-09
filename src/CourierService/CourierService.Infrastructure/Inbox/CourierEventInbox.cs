using Microsoft.EntityFrameworkCore;
using CourierService.Infrastructure.Persistence;
using Shared.Contracts.Events;
using Shared.Services;

namespace CourierService.Infrastructure.Inbox;

public sealed class CourierEventInbox : IEventInbox
{
    private readonly CourierDbContext _db;

    public CourierEventInbox(CourierDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryStartAsync(
        string eventId,
        string eventType,
        Guid aggregateId,
        string topic,
        int partition,
        long offset,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return false;

        var exists = await _db.ProcessedEvents.AsNoTracking()
            .AnyAsync(x => x.EventId == eventId, ct);
        if (exists)
            return false;

        var entry = new ProcessedEvent
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = eventType,
            AggregateId = aggregateId,
            Topic = topic,
            Partition = partition,
            Offset = offset,
            Attempts = 1,
            Status = "processing",
            ReceivedAt = DateTime.UtcNow
        };

        _db.ProcessedEvents.Add(entry);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task MarkProcessedAsync(string eventId, CancellationToken ct = default)
    {
        var entry = await _db.ProcessedEvents.FirstOrDefaultAsync(x => x.EventId == eventId, ct);
        if (entry is null) return;
        entry.Status = "processed";
        entry.ProcessedAt = DateTime.UtcNow;
        entry.Error = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkFailedAsync(string eventId, string error, CancellationToken ct = default)
    {
        var entry = await _db.ProcessedEvents.FirstOrDefaultAsync(x => x.EventId == eventId, ct);
        if (entry is null) return;
        entry.Status = "failed";
        entry.ProcessedAt = DateTime.UtcNow;
        entry.Error = error;
        entry.Attempts += 1;
        await _db.SaveChangesAsync(ct);
    }
}
