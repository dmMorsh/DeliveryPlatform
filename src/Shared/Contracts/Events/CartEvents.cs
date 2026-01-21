namespace Shared.Contracts.Events;

public class CartItemAddedEvent : IntegrationEvent
{
    public Guid CartId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }

    public override string EventType => "cart.item.added";
    public override int Version => 1;
    public override string AggregateType => "cart";
    public override Guid AggregateId => CartId;
}

public class CartCheckedOutEvent : IntegrationEvent
{
    public Guid CartId { get; set; }
    public Guid CustomerId { get; set; }

    public override string EventType => "cart.checked_out";
    public override int Version => 1;
    public override string AggregateType => "cart";
    public override Guid AggregateId => CartId;
}
