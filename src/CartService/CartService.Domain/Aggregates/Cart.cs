using CartService.Domain.Entities;
using CartService.Domain.Events;
using CartService.Domain.SeedWork;

namespace CartService.Domain.Aggregates;

public class Cart : AggregateRoot
{
    public Guid CustomerId { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Guid CheckoutId { get; private set; }

    private readonly List<CartItem> _items = new();
    public IReadOnlyCollection<CartItem> Items => _items;

    private Cart() { }

    public Cart(Guid customerId)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        CheckoutId = Guid.NewGuid();
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddItem(Guid productId, string name, int priceCents, int quantity)
    {
        var item = new CartItem(productId, name, priceCents, quantity);
        _items.Add(item);
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CartItemAddedDomainEvent { CartId = Id, ProductId = item.ProductId, Quantity = item.Quantity });
    }
    
    public void RemoveItem(CartItem item)
    {
        _items.Remove(item);
        UpdatedAt = DateTime.UtcNow;
        AddDomainEvent(new CartItemRemovedDomainEvent { CartId = Id, ProductId = item.ProductId, Quantity = item.Quantity });
    }

    public void Clear() => _items.Clear();

    public void Checkout(Guid orderId)
    {
        AddDomainEvent(new CartCheckedOutDomainEvent { CartId = Id, CustomerId = CustomerId, OrderId = orderId});
        _items.Clear();
        CheckoutId = Guid.NewGuid();
        UpdatedAt = DateTime.UtcNow;
    }
}
