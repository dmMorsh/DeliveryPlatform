namespace Shared.Contracts.Events;

public sealed record CommandFailedEvent : IntegrationEvent
{
    private readonly string _aggregateType;

    public CommandFailedEvent(string aggregateType)
    {
        if (string.IsNullOrWhiteSpace(aggregateType))
            throw new ArgumentException("Aggregate type is required.", nameof(aggregateType));

        _aggregateType = aggregateType;
    }

    public override string EventType => $"{_aggregateType}.command_failed";
    public override int Version => 1;
    public override string AggregateType => _aggregateType;
    public override Guid AggregateId => CorrelationId;

    public Guid CorrelationId { get; init; }
    public string CommandType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public int RetryCount { get; init; }
    public DateTime FailedAt { get; init; }
}