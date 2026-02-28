using InventoryService.Application.Read;
using InventoryService.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.ReadStore;

public class InventoryReadProjector
{
    private readonly InventoryReadDbContext _context;
    private readonly IInventoryReadCache _cache;

    public InventoryReadProjector(InventoryReadDbContext context, IInventoryReadCache cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task HandleAsync(StockReservedDomainEvent evt, CancellationToken ct)
    {
        var item = await _context.StockItems.FirstOrDefaultAsync(x => x.ProductId == evt.ProductId, ct);
        if (item == null)
        {
            item = new StockItemReadModel
            {
                ProductId = evt.ProductId,
                TotalQuantity = 0,
                ReservedQuantity = evt.Quantity,
                AvailableQuantity = -evt.Quantity
            };
            _context.StockItems.Add(item);
        }
        else
        {
            item.ReservedQuantity += evt.Quantity;
            item.AvailableQuantity -= evt.Quantity;
            _context.StockItems.Update(item);
        }

        await _context.SaveChangesAsync(ct);
        await _cache.RemoveAsync(evt.ProductId, ct);
    }

    public async Task HandleAsync(StockReleasedDomainEvent evt, CancellationToken ct)
    {
        var item = await _context.StockItems.FirstOrDefaultAsync(x => x.ProductId == evt.ProductId, ct);
        if (item == null)
        {
            item = new StockItemReadModel
            {
                ProductId = evt.ProductId,
                TotalQuantity = 0,
                ReservedQuantity = 0,
                AvailableQuantity = 0
            };
            _context.StockItems.Add(item);
        }
        else
        {
            item.ReservedQuantity -= evt.Quantity;
            item.AvailableQuantity += evt.Quantity;
            _context.StockItems.Update(item);
        }

        await _context.SaveChangesAsync(ct);
        await _cache.RemoveAsync(evt.ProductId, ct);
    }
}
