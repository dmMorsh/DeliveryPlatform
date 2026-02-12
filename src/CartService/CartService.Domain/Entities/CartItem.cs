using CartService.Domain.SeedWork;

namespace CartService.Domain.Entities;

public class CartItem : Entity
{
    public Guid ProductId { get; init; }
    public string Name { get; private set; } = null!;
    public int PriceCents { get; private set; }
    public int Quantity { get; private set; }

    private CartItem() { }

    internal CartItem(Guid productId, string name, int priceCents, int quantity)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Name = name;
        PriceCents = priceCents;
        Quantity = quantity;
    }
}
