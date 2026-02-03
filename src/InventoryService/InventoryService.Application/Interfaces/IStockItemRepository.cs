using InventoryService.Domain.Aggregates;

namespace InventoryService.Application.Interfaces;

public interface IStockItemRepository
{
    Task<StockItem?> GetByProductIdAsync(Guid productId, CancellationToken ct);
    Task AddAsync(StockItem item, CancellationToken ct);
}