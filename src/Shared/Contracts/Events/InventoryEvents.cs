namespace Shared.Contracts.Events;

public record StockReservedEvent : IntegrationEvent
{
    public override string EventType => "stock.reserved";
    public override int Version => 1;
    public override string AggregateType => "inventory";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public IReadOnlyCollection<StockItemSnapshot> Items { get; init; }
}

public record StockReserveFailedEvent : IntegrationEvent
{
    public override string EventType => "stock.reserve_failed";
    public override int Version => 1;
    public override string AggregateType => "inventory";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public IReadOnlyCollection<FailedStockItemSnapshot> Items { get; init; }
    
}

public record StockReleasedEvent : IntegrationEvent
{ 
    public override string EventType => "stock.released";
    public override int Version => 1;
    public override string AggregateType => "inventory";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public IReadOnlyCollection<StockItemSnapshot> Items { get; init; }
}

public record StockReleaseFailedEvent : IntegrationEvent
{
    public override string EventType => "stock.release_failed";
    public override int Version => 1;
    public override string AggregateType => "inventory";
    public override Guid AggregateId => OrderId;
    public required Guid OrderId { get; init; }
    public IReadOnlyCollection<FailedStockItemSnapshot> Items { get; init; }
    
}

public record StockItemSnapshot
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}

public record FailedStockItemSnapshot : StockItemSnapshot
{
    public required string Reason { get; init; }
}
