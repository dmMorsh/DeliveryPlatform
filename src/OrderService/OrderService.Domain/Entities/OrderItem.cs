using OrderService.Domain.SeedWork;

namespace OrderService.Domain.Entities;

public class OrderItem : Entity
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = null!;
    public int PriceCents { get; private set; }
    public int Quantity { get; private set; }
    public OrderItemStatus Status { get; private set; }

    private OrderItem() { }

    public OrderItem(Guid productId, string name, int priceCents, int quantity)
    {
        Id = Guid.NewGuid();
        ProductId = productId;
        Name = name;
        PriceCents = priceCents;
        Quantity = quantity;
        Status = OrderItemStatus.Pending;
    }
}

public enum OrderItemStatus
{
    Pending,
    Reserved,
    ReservationFailed,
    Releasing,
    Released,
    Shipped,
    Lost
}