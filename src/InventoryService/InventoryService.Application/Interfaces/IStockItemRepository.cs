using InventoryService.Domain.Aggregates;

namespace InventoryService.Application.Interfaces;

public interface IStockItemRepository
{
    Task<StockItem?> GetByProductIdAsync(Guid productId, CancellationToken ct);
    void Add(StockItem item);
}