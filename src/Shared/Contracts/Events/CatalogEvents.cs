namespace Shared.Contracts.Events;

public record ProductCreatedEvent : IntegrationEvent
{
    public override string EventType => "product.created";
    public override int Version => 1;
    public override string AggregateType => "product";
    public override Guid AggregateId => ProductId;
    public required Guid ProductId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public long PriceCents { get; init; }
    public int QuantityAvailable { get; init; }
}

public record ProductPriceChangedEvent : IntegrationEvent
{

    public override string EventType => "product.price_changed";
    public override int Version => 1;
    public override string AggregateType => "product";
    public override Guid AggregateId => ProductId;
    public required Guid ProductId { get; init; }
    public long OldPriceCents { get; init; }
    public long NewPriceCents { get; init; }
}
