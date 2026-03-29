namespace Shared.Contracts.Events;

/// <summary>
/// Event: Courier created/registered
/// </summary>
public record CourierRegisteredEvent : IntegrationEvent
{
    public override string EventType => "courier.registered";
    public override int Version => 1;
    public override string AggregateType => "Courier";
    public override Guid AggregateId => CourierId;
    public required Guid CourierId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Event: Courier status changed
/// </summary>
public record CourierStatusChangedEvent : IntegrationEvent
{
    public override string EventType => "courier.status.changed";
    public override int Version => 1;
    public override string AggregateType => "Courier";
    public override Guid AggregateId => CourierId;
    public required Guid CourierId { get; init; }
    public int PreviousStatus { get; init; }
    public int NewStatus { get; init; }
    public DateTime ChangedAt { get; init; }
}

/// <summary>
/// Event: Courier rating updated
/// </summary>
public record CourierRatingUpdatedEvent : IntegrationEvent
{
    public override string EventType => "courier.rating.updated";
    public override int Version => 1;
    public override string AggregateType => "courier";
    public override Guid AggregateId => CourierId;
    public required Guid CourierId { get; init; }
    public double NewRating { get; init; }
    public int TotalRatings { get; init; }
    public string? Feedback { get; init; }
}
