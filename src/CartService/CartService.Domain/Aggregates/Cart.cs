using CartService.Domain.Entities;
using CartService.Domain.Events;
using CartService.Domain.SeedWork;

namespace CartService.Domain.Aggregates;

public class Cart : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items;

    // domain events
    private readonly List<CartDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<CartDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    private void AddDomainEvent(CartDomainEvent e) => _domainEvents.Add(e);
    public void ClearDomainEvents() => _domainEvents.Clear();

    private Cart() { }

    public Cart(Guid customerId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(CartItem item)
    {
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CartItemAddedDomainEvent { CartId = Id, ProductId = item.ProductId, Quantity = item.Quantity });
    }

    public void Clear() => _items.Clear();

    public void Checkout()
    {
        // business rules may be added here
        AddDomainEvent(new CartCheckedOutDomainEvent { CartId = Id, CustomerId = CustomerId });
        _items.Clear();
        UpdatedAt = DateTime.UtcNow;
    }
}
