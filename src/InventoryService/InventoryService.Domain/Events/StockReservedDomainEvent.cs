using InventoryService.Domain.SeedWork;

namespace InventoryService.Domain.Events;

public sealed class StockReservedDomainEvent(Guid ProductId, Guid OrderId, int Quantity ) : DomainEvent;