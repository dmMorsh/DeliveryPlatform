namespace Shared.Contracts.Events;

public sealed class ProcessedEvent
{
    public Guid Id { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public int Partition { get; set; }
    public long Offset { get; set; }
    public int Attempts { get; set; }
    public string Status { get; set; } = "processing";
    public string? Error { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
