using InventoryService.Domain.SeedWork;

namespace InventoryService.Domain.Events;

public sealed class StockReleasedDomainEvent(Guid ProductId, Guid OrderId, int Quantity ) : DomainEvent;