using Shared.Contracts.Events;

namespace InventoryService.Application.Interfaces;

public interface IStockIntegrationEventMapper
{
    StockReservedEvent MapStockReservedEvent(Guid orderId, StockItemSnapshot[] items);
    StockReserveFailedEvent MapStockReserveFailedEvent(Guid orderId, List<FailedStockItemSnapshot> items);
    StockReleasedEvent MapStockReleasedEvent(Guid orderId, StockItemSnapshot[] items);
    StockReleaseFailedEvent MapStockReleaseFailedEvent(Guid orderId, List<FailedStockItemSnapshot> items);
}
