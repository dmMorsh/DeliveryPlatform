using InventoryService.Domain.SeedWork;

namespace InventoryService.Domain.Events;

public sealed record StockReleasedDomainEvent : DomainEvent
{ 
    public Guid ProductId { get; init; }
    public Guid OrderId { get; init; }
    public int Quantity { get; init; }
}