namespace Shared.Contracts.Events;

/// <summary>
/// Event: Заказ создан
/// </summary>
public class OrderCreatedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public double FromLatitude { get; set; }
    public double FromLongitude { get; set; }
    public double ToLatitude { get; set; }
    public double ToLongitude { get; set; }
    public decimal CostCents { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemSnapshot> Items { get; set; } = new();
    
    public override string EventType => "order.created";
    public override int Version => 1;
    public override string AggregateType => "order";
    public override Guid AggregateId => OrderId;
}

/// <summary>
/// Снимок позиции заказа для событий
/// </summary>
public class OrderItemSnapshot
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Price { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
/// Event: Курьер назначен на заказ
/// </summary>
public class OrderAssignedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid CourierId { get; set; }
    public string CourierName { get; set; } = string.Empty;
    public string CourierPhone { get; set; } = string.Empty;
    
    public override string EventType => "order.assigned";
    public override int Version => 1;
    public override string AggregateType => "order";
    public override Guid AggregateId => OrderId;
}

/// <summary>
/// Event: Статус заказа изменился
/// </summary>
public class OrderStatusChangedEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public int PreviousStatus { get; set; }
    public int NewStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    
    public override string EventType => "order.status.changed";
    public override int Version => 1;
    public override string AggregateType => "order";
    public override Guid AggregateId => OrderId;
}

/// <summary>
/// Event: Заказ доставлен
/// </summary>
public class OrderDeliveredEvent : IntegrationEvent
{
    public Guid OrderId { get; set; }
    public Guid CourierId { get; set; }
    public DateTime DeliveredAt { get; set; }
    public string? Signature { get; set; }
    public string? Notes { get; set; }
    
    public override string EventType => "order.delivered";
    public override int Version => 1;
    public override string AggregateType => "order";
    public override Guid AggregateId => OrderId;
}
