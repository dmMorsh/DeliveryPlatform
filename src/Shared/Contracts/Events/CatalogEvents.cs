namespace Shared.Contracts.Events;

public class ProductCreatedEvent : IntegrationEvent
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public long PriceCents { get; set; }
    public int QuantityAvailable { get; set; }

    public override string EventType => "product.created";
    public override int Version => 1;
    public override string AggregateType => "product";
    public override Guid AggregateId 
    { 
        get => ProductId;
        set => ProductId = value;
    }
}

public class ProductPriceChangedEvent : IntegrationEvent
{
    public Guid ProductId { get; set; }
    public long OldPriceCents { get; set; }
    public long NewPriceCents { get; set; }

    public override string EventType => "product.price_changed";
    public override int Version => 1;
    public override string AggregateType => "product";
    public override Guid AggregateId 
    { 
        get => ProductId;
        set => ProductId = value;
    }
}
