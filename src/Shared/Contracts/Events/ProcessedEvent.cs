namespace Shared.Contracts.Events;

public sealed class ProcessedEvent
{
    public required Guid Id { get; set; }
    public required string EventId { get; set; }
    public required string EventType { get; set; }
    public Guid AggregateId { get; set; }
    public required string Topic { get; set; }
    public int Partition { get; set; }
    public long Offset { get; set; }
    public int Attempts { get; set; }
    public string Status { get; set; } = "processing";
    public string? Error { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
