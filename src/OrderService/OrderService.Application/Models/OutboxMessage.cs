using Shared.Contracts.Events;
using Shared.Utilities;

namespace OrderService.Application.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public string? Topic { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? LastError { get; set; }
    
    public static OutboxMessage From(IntegrationEvent evt)
        => new()
        {
            Id = Guid.NewGuid(),
            AggregateId = evt.AggregateId,
            Type = evt.EventType,
            Payload = EventSerializer.SerializeEvent(evt),
            Topic = (evt.AggregateType ?? "events").ToLowerInvariant() + ".events",
            OccurredAt = evt.Timestamp
        };
}
