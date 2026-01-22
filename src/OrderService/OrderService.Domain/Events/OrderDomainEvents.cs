using OrderService.Domain.SeedWork;

namespace OrderService.Domain.Events;

public class OrderCreatedDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
}

public class OrderAssignedDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public Guid CourierId { get; init; }
}

public class OrderStatusChangedDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public OrderStatus PreviousStatus { get; init; }
    public OrderStatus NewStatus { get; init; }
}
