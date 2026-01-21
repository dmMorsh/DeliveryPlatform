namespace CourierService.Infrastructure;

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
}
