using InventoryService.Application.Interfaces;
using Shared.Contracts.Events;

namespace InventoryService.Infrastructure.Mapping;

public class StockIntegrationEventMapper : IStockIntegrationEventMapper
{
    public StockReservedEvent MapStockReservedEvent(Guid orderId, StockItemSnapshot[] items)
    {
        return new StockReservedEvent { OrderId = orderId, Items = items };
    }
    
    public StockReserveFailedEvent MapStockReserveFailedEvent(Guid orderId, List<FailedStockItemSnapshot> items)
    {
        return new StockReserveFailedEvent { OrderId = orderId, Items = items };
    }

    public StockReleasedEvent MapStockReleasedEvent(Guid orderId, StockItemSnapshot[] items)
    {
        return new StockReleasedEvent { OrderId = orderId, Items = items };
    }

    public StockReleaseFailedEvent MapStockReleaseFailedEvent(Guid orderId, List<FailedStockItemSnapshot> items)
    {
        return new StockReleaseFailedEvent {OrderId = orderId, Items = items};
    }
}
