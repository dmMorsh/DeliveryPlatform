using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Contracts.Events;
using Shared.Services;

namespace OrderService.Infrastructure.ReadStore;

public sealed class OrderReadEventInbox : IEventInbox
{
    private readonly OrderReadDbContext _db;

    public OrderReadEventInbox(OrderReadDbContext db)
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

        var existing = await _db.ProcessedEvents
            .FirstOrDefaultAsync(x => x.EventId == eventId, ct);
        if (existing != null)
        {
            if (existing.Status == "failed")
            {
                existing.Status = "processing";
                existing.Error = null;
                existing.EventType = eventType;
                existing.AggregateId = aggregateId;
                existing.Topic = topic;
                existing.Partition = partition;
                existing.Offset = offset;
                existing.ReceivedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
                return true;
            }

            return false;
        }

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
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return false;
        }
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

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg
               && pg.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
