using InventoryService.Application.Models;

namespace InventoryService.Application.Read;

public interface IInventoryReadRepository
{
    Task<StockItemView?> GetByProductIdAsync(Guid productId, CancellationToken ct);
    Task<List<StockItemView>> GetAllAsync(CancellationToken ct);
}
