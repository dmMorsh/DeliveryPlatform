using InventoryService.Application.Interfaces;
using Shared.Contracts.Events;

namespace InventoryService.Infrastructure.Mapping;

public class StockIntegrationEventMapper : IStockIntegrationEventMapper
{
    public StockReservedEvent MapStockReservedEvent(Guid productId, Guid orderId, int quantity)
    {
        return new StockReservedEvent
        {
            ProductId = productId,
            OrderId = orderId,
            Quantity = quantity
        };
    }

    public StockReleasedEvent MapStockReleasedEvent(Guid productId, Guid orderId, int quantity)
    {
        return new StockReleasedEvent
        {
            ProductId = productId,
            OrderId = orderId,
            Quantity = quantity
        };
    }
}
