using OrderService.Domain.SeedWork;

namespace OrderService.Domain.Events;

public class OrderCreatedDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public Guid AggregateId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public decimal CostCents { get; set; }
    public string? Description { get; set; }
    
    public List<OrderItemSnapshot> Items { get; set; } = new();
}

public class OrderAssignedDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public Guid CourierId { get; init; }
    public Guid AggregateId { get; set; }
}

public class OrderStatusChangedDomainEvent : DomainEvent
{
    public Guid OrderId { get; init; }
    public OrderStatus PreviousStatus { get; init; }
    public OrderStatus NewStatus { get; init; }
    public Guid AggregateId { get; set; }
}

public class OrderItemSnapshot
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public long PriceCents { get; set; }
    public int Quantity { get; set; }
}