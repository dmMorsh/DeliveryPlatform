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
    public long CostCents { get; init; }
    public string? Description { get; init; }

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

public record DomainOrderItemSnapshot
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public long PriceCents { get; init; }
    public int Quantity { get; init; }
}