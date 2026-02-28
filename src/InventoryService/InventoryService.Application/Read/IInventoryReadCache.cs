using InventoryService.Application.Models;

namespace InventoryService.Application.Read;

public interface IInventoryReadCache
{
    Task<StockItemView?> GetAsync(Guid productId, CancellationToken ct);
    Task SetAsync(Guid productId, StockItemView view, CancellationToken ct);
    Task RemoveAsync(Guid productId, CancellationToken ct);
}
