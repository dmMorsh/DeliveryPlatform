using InventoryService.Domain.SeedWork;

namespace InventoryService.Domain.Events;

public sealed class StockReleasedDomainEvent : DomainEvent
{ 
    public Guid ProductId { get; set; }
    public Guid OrderId { get; set; }
    public int Quantity { get; set; }
}