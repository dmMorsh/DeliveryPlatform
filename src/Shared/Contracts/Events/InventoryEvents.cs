namespace Shared.Contracts.Events;

public class StockReservedEvent : IntegrationEvent
{
    public Guid ProductId { get; set; }
    public Guid OrderId { get; set; }
    public int Quantity { get; set; }

    public override string EventType => "stock.reserved";
    public override int Version => 1;
    public override string AggregateType => "inventory";
    public override Guid AggregateId 
    { 
        get => ProductId;
        set => ProductId = value;
    }
}

public class StockReleasedEvent : IntegrationEvent
{
    public Guid ProductId { get; set; }
    public Guid OrderId { get; set; }
    public int Quantity { get; set; }

    public override string EventType => "stock.released";
    public override int Version => 1;
    public override string AggregateType => "inventory";
    public override Guid AggregateId 
    { 
        get => ProductId;
        set => ProductId = value;
    }
}
