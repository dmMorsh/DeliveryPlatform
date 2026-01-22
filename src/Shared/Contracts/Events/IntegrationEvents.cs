namespace Shared.Contracts.Events;

public abstract class IntegrationEvent
{
    /// <summary>Timestamp события</summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>Тип события</summary>
    public abstract string EventType { get; }
    
    /// <summary>Уникальный идентификатор события</summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    
    
    /// <summary>Версия события для evolving schema</summary>
    public abstract int Version { get; }
    
    /// <summary>Тип корневого агрегата (Order, Courier, etc.)</summary>
    public abstract string AggregateType { get; }
    
    /// <summary>ID корневого агрегата</summary>
    public abstract Guid AggregateId { get; }
}

// public class OrderItemSnapshot
// {
//     public Guid ProductId { get; set; }
//     public string Name { get; set; } = string.Empty;
//     public int Price { get; set; }
//     public int Quantity { get; set; }
// }

// public class OrderCreatedEvent : IntegrationEvent
// {
//     public override string EventType => "OrderCreated";
//     public Guid OrderId { get; set; }
//     public string? OrderNumber { get; set; }
//     public Guid ClientId { get; set; }
//     public string FromAddress { get; set; } = string.Empty;
//     public string ToAddress { get; set; } = string.Empty;
//     public double FromLatitude { get; set; }
//     public double FromLongitude { get; set; }
//     public double ToLatitude { get; set; }
//     public double ToLongitude { get; set; }
//     public long CostCents { get; set; }
//     public DateTime CreatedAt { get; set; }
//     public List<OrderItemSnapshot> Items { get; set; } = new();
// }

// public class OrderAssignedEvent : IntegrationEvent
// {
//     public override string EventType => "OrderAssigned";
//     public Guid OrderId { get; set; }
//     public Guid CourierId { get; set; }
// }

// public class OrderStatusChangedEvent : IntegrationEvent
// {
//     public override string EventType => "OrderStatusChanged";
//     public Guid OrderId { get; set; }
//     public int PreviousStatus { get; set; }
//     public int NewStatus { get; set; }
// }
