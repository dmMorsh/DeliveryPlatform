namespace Shared.Contracts.Events;

public record CartItemAddedEvent : IntegrationEvent
{
    public override string EventType => "cart.item.added";
    public override int Version => 1;
    public override string AggregateType => "cart";
    public override Guid AggregateId => CartId;
    public required Guid CartId { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}

public record CartCheckedOutEvent : IntegrationEvent
{
    public override string EventType => "cart.checked_out";
    public override int Version => 1;
    public override string AggregateType => "cart";
    public override Guid AggregateId => CartId;
    public required Guid CartId { get; init; }
    public Guid CustomerId { get; init; }
}
