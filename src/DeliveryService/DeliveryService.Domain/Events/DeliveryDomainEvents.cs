using DeliveryService.Domain.SeedWork;

namespace DeliveryService.Domain.Events;

public record DeliveryCreatedDomainEvent : DomainEvent
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
}

public record DeliveryAssignedDomainEvent : DomainEvent
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
    public Guid CourierId { get; init; }

    // ETA timestamps taken from the aggregate state at the moment of assignment
    public DateTime? EstimatedPickupAt { get; init; }
    public DateTime? EstimatedDeliveryAt { get; init; }
}

public record DeliveryAcceptedDomainEvent : DomainEvent
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
    public Guid CourierId { get; init; }
}

public record DeliveryDeclinedDomainEvent : DomainEvent
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
    public Guid CourierId { get; init; }
    public string? Reason { get; init; }
}

public record DeliveryPickedUpDomainEvent : DomainEvent
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
    public Guid CourierId { get; init; }
}

public record DeliveryInTransitDomainEvent : DomainEvent
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
    public Guid CourierId { get; init; }
}

public record DeliveryDeliveredDomainEvent : DomainEvent
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
    public Guid CourierId { get; init; }
    public string? Signature { get; init; }
    public string? PhotoUrl { get; init; }
    public string? Notes { get; init; }
}

public record DeliveryCancelledDomainEvent : DomainEvent
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
    public Guid? CourierId { get; init; }
    public string? Reason { get; init; }
}

public record DeliveryFailedDomainEvent : DomainEvent
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
    public Guid? CourierId { get; init; }
    public string? Reason { get; init; }
}

public record DeliveryReturnedDomainEvent : DomainEvent
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
    public Guid? CourierId { get; init; }
    public string? Reason { get; init; }
}

public record DeliveryReassignRequestedDomainEvent : DomainEvent
{
    public Guid DeliveryId { get; init; }
    public Guid OrderId { get; init; }
    public Guid? PreviousCourierId { get; init; }
    public string? Reason { get; init; }
}
