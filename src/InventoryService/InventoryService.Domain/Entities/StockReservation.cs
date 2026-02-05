using InventoryService.Domain.SeedWork;

namespace InventoryService.Domain.Entities;

public class StockReservation : Entity
{
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; } // StockItemId
    public int Quantity { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ReleasedAt { get; set; }
}