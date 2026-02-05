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
        return await _context.StockItems.FirstOrDefaultAsync(si => si.Id == productId, ct);
    }
    
    public async Task<List<StockItem>> GetByProductIdsAsync(List<Guid> guids, CancellationToken ct)
    {
        return await _context.StockItems
            .Where(si => guids.Contains(si.Id))
            .ToListAsync(ct);
    }
    
    public async Task<List<StockItem>> GetAllProductAsync(CancellationToken ct)
    {
        return await _context.StockItems
            .ToListAsync(ct);
    }

    public async Task AddAsync(StockItem item, CancellationToken ct)
    {
        await _context.StockItems.AddAsync(item, ct);
    }
    
    public async Task AddRangeAsync(StockItem[] items, CancellationToken ct)
    {
        await _context.StockItems.AddRangeAsync(items, ct);
    }
}
