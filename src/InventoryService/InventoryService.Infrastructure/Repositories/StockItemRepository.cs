using InventoryService.Application.Interfaces;
using InventoryService.Domain.Aggregates;
using InventoryService.Infrastructure.Persistence;

namespace InventoryService.Infrastructure.Repositories;

public class StockItemRepository : IStockItemRepository
{
    private readonly InventoryDbContext _context;

    public StockItemRepository(InventoryDbContext context)
    {
        _context = context;
    }
    
    public Task<StockItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        var item = _context.StockItems.FirstOrDefault(x => x.ProductId == productId);
        return Task.FromResult(item);
    }

    public void Add(StockItem item)
    {
        _context.Add(item);
    }
}
