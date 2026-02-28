using InventoryService.Application.Models;
using InventoryService.Application.Read;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.ReadStore;

public class InventoryReadRepository : IInventoryReadRepository
{
    private readonly InventoryReadDbContext _context;
    private readonly IInventoryReadCache _cache;

    public InventoryReadRepository(InventoryReadDbContext context, IInventoryReadCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<StockItemView?> GetByProductIdAsync(Guid productId, CancellationToken ct)
    {
        // Try cache first
        var cached = await _cache.GetAsync(productId, ct);
        if (cached != null) return cached;

        // Fallback to DB
        var model = await _context.StockItems.FirstOrDefaultAsync(x => x.ProductId == productId, ct);
        if (model == null) return null;

        var view = new StockItemView
        {
            ProductId = model.ProductId,
            TotalQuantity = model.TotalQuantity,
            ReservedQuantity = model.ReservedQuantity,
            AvailableQuantity = model.AvailableQuantity
        };

        // Store in cache
        await _cache.SetAsync(productId, view, ct);
        return view;
    }

    public async Task<List<StockItemView>> GetAllAsync(CancellationToken ct)
    {
        return await _context.StockItems
            .AsNoTracking()
            .Select(m => new StockItemView
            {
                ProductId = m.ProductId,
                TotalQuantity = m.TotalQuantity,
                ReservedQuantity = m.ReservedQuantity,
                AvailableQuantity = m.AvailableQuantity
            })
            .ToListAsync(ct);
    }
}
