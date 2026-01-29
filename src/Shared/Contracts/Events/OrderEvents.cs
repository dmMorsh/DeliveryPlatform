namespace Shared.Contracts.Events;

/// <summary>
/// Event: Заказ создан
/// </summary>
public record OrderCreatedEvent : IntegrationEvent
{
    public override string EventType => "order.created";
    public override int Version => 1;
    public override string AggregateType => "Order";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public string? OrderNumber { get; init; }
    public Guid ClientId { get; init; }
    public string FromAddress { get; init; } = string.Empty;
    public string ToAddress { get; init; } = string.Empty;
    public double FromLatitude { get; init; }
    public double FromLongitude { get; init; }
    public double ToLatitude { get; init; }
    public double ToLongitude { get; init; }
    public long CostCents { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<OrderEItemSnapshot> Items { get; init; } = new();
    public string? Description { get; init; }
}

/// <summary>
/// Снимок позиции заказа для событий
/// </summary>
public record OrderEItemSnapshot
{
    public Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public long PriceCents { get; init; }
    public int Quantity { get; init; }
}

/// <summary>
/// Event: Курьер назначен на заказ
/// </summary>
public record OrderAssignedEvent : IntegrationEvent
{
    public override string EventType => "order.assigned";
    public override int Version => 1;
    public override string AggregateType => "Order";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public Guid CourierId { get; init; }
    public string CourierName { get; init; } = string.Empty;
    public string? CourierPhone { get; init; }
}

public record OrderStatusChangedEvent : IntegrationEvent
{
    public override string EventType => "order.status.changed";
    public override int Version => 1;
    public override string AggregateType => "Order";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public int PreviousStatus { get; init; }
    public int NewStatus { get; init; }
    public string Reason { get; init; } = string.Empty;
    public int OldStatus { get; init; }
    public DateTime ChangedAt { get; init; }
}

/// <summary>
/// Event: Заказ доставлен
/// </summary>
public record OrderDeliveredEvent : IntegrationEvent
{
    public override string EventType => "order.delivered";
    public override int Version => 1;
    public override string AggregateType => "Order";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public Guid CourierId { get; init; }
    public DateTime DeliveredAt { get; init; }
    public string? Signature { get; init; }
    public string? Notes { get; init; }
}
