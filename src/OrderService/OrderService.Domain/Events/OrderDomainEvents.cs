using OrderService.Domain.Aggregates;
using OrderService.Domain.SeedWork;

namespace OrderService.Domain.Events;

public record OrderCreatedDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid ClientId { get; init; }
    public string FromAddress { get; init; } = string.Empty;
    public string ToAddress { get; init; } = string.Empty;
    public double FromLatitude { get; init; }
    public double FromLongitude { get; init; }
    public double ToLatitude { get; init; }
    public double ToLongitude { get; init; }
    public int WeightGrams { get; init; }
    public long CostCents { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? CourierNote { get; init; }
    public string? Description { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ExpectedReadyAt { get; init; }
    public DateTime? KitchenSlotStart { get; init; }
    public string? DeliveryZoneId { get; init; }
    public string? DeliveryZoneName { get; init; }
    public double? DeliveryZoneDistanceKm { get; init; }
    public int? DeliveryPickupSlaMinutes { get; init; }
    public int? DeliveryTransitSlaMinutes { get; init; }
    public double? DeliveryFeeMultiplier { get; init; }

    public required IReadOnlyList<DomainOrderItemSnapshot> Items { get; init; }
}

public record OrderItemsReleaseDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public required IReadOnlyList<DomainOrderItemSnapshot> Items { get; init; }
}

public record OrderAssignedDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public Guid CourierId { get; init; }
}

public record OrderStatusChangedDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public OrderStatus PreviousStatus { get; init; }
    public OrderStatus NewStatus { get; init; }
}

public record OrderReadyDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public DateTime ReadyAt { get; init; }
}

public record OrderAcceptedDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public DateTime AcceptedAt { get; init; }
}

public record OrderRejectedDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public DateTime RejectedAt { get; init; }
    public string? Reason { get; init; }
}

public record OrderCanceledDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public Guid? CourierId { get; init; }
    public string? Reason { get; init; }
}

public record DomainOrderItemSnapshot
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public long PriceCents { get; init; }
    public int Quantity { get; init; }
}

public record OrderCriticalErrorDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public Guid ClientId { get; init; }
    public string? Description { get; init; }
}
