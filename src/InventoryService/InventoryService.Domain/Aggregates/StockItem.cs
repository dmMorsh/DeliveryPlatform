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

    public string? CanReserve(int quantity)
    {
        if (quantity <= 0)
            return "Quantity must be positive";
        if (AvailableQuantity < quantity)
            return $"Not enough stock, {AvailableQuantity} available, {quantity} required";
        
        return null;
    }

    public void Reserve(int quantity, Guid orderId, bool checkAvailability = true)
    {
        if (checkAvailability) //in case we forget check before
        {
            var error = CanReserve(quantity);
            if (error != null)
                throw new DomainException(error);
        }
        
        ReservedQuantity += quantity;

        AddDomainEvent(new StockReservedDomainEvent{
            ProductId = ProductId,
            OrderId = orderId,
            Quantity = quantity});
    }

    public string? CanRelease(int quantity)
    {
        if (quantity <= 0)
            return "Quantity must be positive";

        if (ReservedQuantity < quantity)
            return $"Cannot release more than reserved, {ReservedQuantity} available, {quantity} required";
        return null;
    }

    public void Release(int quantity, Guid orderId, bool checkAvailability = true)
    {
        if (checkAvailability) //in case we forget check before
        {
            var error = CanRelease(quantity);
            if (error != null)
                throw new DomainException(error);
        }        

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
