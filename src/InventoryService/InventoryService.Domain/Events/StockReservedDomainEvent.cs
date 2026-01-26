using InventoryService.Domain.SeedWork;

namespace InventoryService.Domain.Events;

public sealed class StockReservedDomainEvent : DomainEvent
{
    public Guid ProductId { get; set; }
    public Guid OrderId { get; set; }
    public int Quantity { get; set; }
}