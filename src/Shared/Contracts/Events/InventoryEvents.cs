namespace Shared.Contracts.Events;

public record StockReservedEvent : IntegrationEvent
{
    public override string EventType => "stock.reserved";
    public override int Version => 1;
    public override string AggregateType => "inventory";
    public override Guid AggregateId => ProductId;
    public required Guid ProductId { get; init; }
    public Guid OrderId { get; init; }
    public int Quantity { get; init; }
}

public record StockReleasedEvent : IntegrationEvent
{ 
    public override string EventType => "stock.released";
    public override int Version => 1;
    public override string AggregateType => "inventory";
    public override Guid AggregateId => ProductId;
    public required Guid ProductId { get; init; }
    public Guid OrderId { get; init; }
    public int Quantity { get; init; }
}
