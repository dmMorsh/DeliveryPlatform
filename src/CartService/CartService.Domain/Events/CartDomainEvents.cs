using CartService.Domain.SeedWork;

namespace CartService.Domain.Events;

public record CartItemAddedDomainEvent : DomainEvent
{
    public Guid CartId { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}

public record CartCheckedOutDomainEvent : DomainEvent
{
    public Guid CartId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid OrderId { get; init; }
}
