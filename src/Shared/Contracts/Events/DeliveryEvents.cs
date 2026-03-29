namespace Shared.Contracts.Events;

/// <summary>
/// Event: Delivery created
/// </summary>
public record DeliveryCreatedEvent : IntegrationEvent
{
    public override string EventType => "delivery.created";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
}

/// <summary>
/// Event: Courier assigned to delivery
/// </summary>
public record DeliveryAssignedEvent : IntegrationEvent
{
    public override string EventType => "delivery.assigned";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid CourierId { get; init; }

    // optional ETAs calculated at assignment time; consumer can update read models / notify suppliers
    public DateTime? EstimatedPickupAt { get; init; }
    public DateTime? EstimatedDeliveryAt { get; init; }
}

/// <summary>
/// Event: Courier accepted delivery
/// </summary>
public record DeliveryAcceptedEvent : IntegrationEvent
{
    public override string EventType => "delivery.accepted";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid CourierId { get; init; }
}

/// <summary>
/// Event: Courier declined delivery
/// </summary>
public record DeliveryDeclinedEvent : IntegrationEvent
{
    public override string EventType => "delivery.declined";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid CourierId { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Event: Courier picked up the order
/// </summary>
public record DeliveryPickedUpEvent : IntegrationEvent
{
    public override string EventType => "delivery.picked_up";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid CourierId { get; init; }
}

/// <summary>
/// Event: Delivery in transit
/// </summary>
public record DeliveryInTransitEvent : IntegrationEvent
{
    public override string EventType => "delivery.in_transit";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid CourierId { get; init; }
}

/// <summary>
/// Event: Delivery completed
/// </summary>
public record DeliveryDeliveredEvent : IntegrationEvent
{
    public override string EventType => "delivery.delivered";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public required Guid CourierId { get; init; }
    public string? Signature { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Notes { get; init; }
}

/// <summary>
/// Event: Delivery canceled
/// </summary>
public record DeliveryCancelledEvent : IntegrationEvent
{
    public override string EventType => "delivery.cancelled";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public Guid? CourierId { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Event: Delivery failed
/// </summary>
public record DeliveryFailedEvent : IntegrationEvent
{
    public override string EventType => "delivery.failed";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public Guid? CourierId { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Event: Delivery returned
/// </summary>
public record DeliveryReturnedEvent : IntegrationEvent
{
    public override string EventType => "delivery.returned";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public Guid? CourierId { get; init; }
    public string? Reason { get; init; }
}

public record DeliveryPickupTimeoutEvent : IntegrationEvent
{
    public override string EventType => "delivery.pickup_timeout";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public Guid? CourierId { get; init; }
    public DateTime? AssignedAt { get; init; }
    public DateTime DetectedAt { get; init; }
}

public record DeliveryInTransitTimeoutEvent : IntegrationEvent
{
    public override string EventType => "delivery.in_transit_timeout";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public Guid? CourierId { get; init; }
    public DateTime? InTransitAt { get; init; }
    public DateTime DetectedAt { get; init; }
}

public record DeliveryReassignRequestedEvent : IntegrationEvent
{
    public override string EventType => "delivery.reassign_requested";
    public override int Version => 1;
    public override string AggregateType => "Delivery";
    public override Guid AggregateId => DeliveryId;
    public required Guid DeliveryId { get; init; }
    public required Guid OrderId { get; init; }
    public Guid? PreviousCourierId { get; init; }
    public string? Reason { get; init; }
}
