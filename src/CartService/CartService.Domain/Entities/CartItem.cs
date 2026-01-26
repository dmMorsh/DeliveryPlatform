using CartService.Domain.SeedWork;

namespace CartService.Domain.Entities;

public class CartItem : Entity
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = null!;
    public int Price { get; private set; }
    public int Quantity { get; private set; }

    private CartItem() { }

    public CartItem(Guid productId, string name, int price, int quantity)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Name = name;
        Price = price;
        Quantity = quantity;
    }
}
