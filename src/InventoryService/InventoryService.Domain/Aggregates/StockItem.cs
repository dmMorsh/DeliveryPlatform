using InventoryService.Domain.Events;
using InventoryService.Domain.SeedWork;

namespace InventoryService.Domain.Aggregates;

public class StockItem : AggregateRoot
{
    public Guid ProductId { get; private set; }

    public int TotalQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }

    public int AvailableQuantity => TotalQuantity - ReservedQuantity;

    private StockItem() { }

    public StockItem(Guid productId, int initialQuantity)
    {
        if (initialQuantity < 0)
            throw new DomainException("Initial quantity cannot be negative");

        Id = Guid.NewGuid();
        ProductId = productId;
        TotalQuantity = initialQuantity;
        ReservedQuantity = 0;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        TotalQuantity += quantity;
    }

    public void Reserve(int quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        if (AvailableQuantity < quantity)
            throw new DomainException("Not enough stock");

        ReservedQuantity += quantity;

        AddDomainEvent(new StockReservedDomainEvent{
            ProductId = ProductId,
            OrderId = orderId,
            Quantity = quantity});
    }

    public void Release(int quantity, Guid orderId)
    {
        if (quantity <= 0)
            throw new DomainException("Quantity must be positive");

        if (ReservedQuantity < quantity)
            throw new DomainException("Cannot release more than reserved");

        ReservedQuantity -= quantity;

        AddDomainEvent(new StockReleasedDomainEvent{
            ProductId = ProductId,
            OrderId = orderId,
            Quantity = quantity});
    }

    public void CommitReservation(int quantity)
    {
        ReservedQuantity -= quantity;
        TotalQuantity -= quantity;
    }
}
