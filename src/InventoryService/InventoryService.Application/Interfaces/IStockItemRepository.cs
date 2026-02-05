using InventoryService.Domain.Aggregates;

namespace InventoryService.Application.Interfaces;

public interface IStockItemRepository
{
    Task<StockItem?> GetByProductIdAsync(Guid productId, CancellationToken ct);
    Task<List<StockItem>> GetByProductIdsAsync(List<Guid> guids, CancellationToken ct);
    Task<List<StockItem>> GetAllProductAsync(CancellationToken ct);
    Task AddAsync(StockItem item, CancellationToken ct);
    Task AddRangeAsync(StockItem[] items, CancellationToken ct);
}