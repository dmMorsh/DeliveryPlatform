using InventoryService.Application.Interfaces;
using InventoryService.Domain.Aggregates;
using InventoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Repositories;

public class StockItemRepository : IStockItemRepository
{
    private readonly InventoryDbContext _context;

    public StockItemRepository(InventoryDbContext context)
    {
        _context = context;
    }
    
    public async Task<StockItem?> GetByProductIdAsync(Guid productId, CancellationToken ct = default)
    {
        return await _context.StockItems.FirstOrDefaultAsync(x => x.ProductId == productId);
    }

    public async Task AddAsync(StockItem item, CancellationToken ct)
    {
        await _context.AddAsync(item);
    }
}
