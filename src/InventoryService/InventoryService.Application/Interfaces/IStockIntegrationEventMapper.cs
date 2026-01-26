using Shared.Contracts.Events;

namespace InventoryService.Application.Interfaces;

public interface IStockIntegrationEventMapper
{
    StockReservedEvent MapStockReservedEvent(Guid productId, Guid orderId, int quantity);
    StockReleasedEvent MapStockReleasedEvent(Guid productId, Guid orderId, int quantity);
}
