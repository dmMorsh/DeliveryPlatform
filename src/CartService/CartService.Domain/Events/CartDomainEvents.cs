using CartService.Domain.SeedWork;

namespace CartService.Domain.Events;

public abstract class CartDomainEvent : DomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public class CartItemAddedDomainEvent : CartDomainEvent
{
    public Guid CartId { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}

public class CartCheckedOutDomainEvent : CartDomainEvent
{
    public Guid CartId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid OrderId { get; init; }
}
