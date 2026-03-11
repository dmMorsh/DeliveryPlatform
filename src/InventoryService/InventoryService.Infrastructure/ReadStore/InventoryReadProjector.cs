using InventoryService.Application.Read;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;

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

    public async Task HandleAsync(StockReservedEvent evt, CancellationToken ct)
    {
        if (evt.Items == null || evt.Items.Count == 0)
            return;

        var items = evt.Items.ToArray();
        var productIds = items.Select(i => i.ProductId).ToList();
        var existing = await _context.StockItems
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(ct);
        var existingById = existing.ToDictionary(x => x.ProductId);

        foreach (var item in items)
        {
            if (!existingById.TryGetValue(item.ProductId, out var model))
            {
                model = new StockItemReadModel
                {
                    ProductId = item.ProductId,
                    TotalQuantity = 0,
                    ReservedQuantity = item.Quantity,
                    AvailableQuantity = 0 - item.Quantity
                };
                _context.StockItems.Add(model);
                continue;
            }

            model.ReservedQuantity += item.Quantity;
            model.AvailableQuantity = model.TotalQuantity - model.ReservedQuantity;
            _context.StockItems.Update(model);
        }

        await _context.SaveChangesAsync(ct);
        foreach (var item in items)
            await _cache.RemoveAsync(item.ProductId, ct);
    }

    public async Task HandleAsync(StockReleasedEvent evt, CancellationToken ct)
    {
        if (evt.Items == null || evt.Items.Count == 0)
            return;

        var items = evt.Items.ToArray();
        var productIds = items.Select(i => i.ProductId).ToList();
        var existing = await _context.StockItems
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(ct);
        var existingById = existing.ToDictionary(x => x.ProductId);

        foreach (var item in items)
        {
            if (!existingById.TryGetValue(item.ProductId, out var model))
            {
                model = new StockItemReadModel
                {
                    ProductId = item.ProductId,
                    TotalQuantity = 0,
                    ReservedQuantity = 0,
                    AvailableQuantity = 0
                };
                _context.StockItems.Add(model);
                continue;
            }

            model.ReservedQuantity -= item.Quantity;
            model.AvailableQuantity = model.TotalQuantity - model.ReservedQuantity;
            _context.StockItems.Update(model);
        }

        await _context.SaveChangesAsync(ct);
        foreach (var item in items)
            await _cache.RemoveAsync(item.ProductId, ct);
    }

    public async Task HandleAsync(StockQuantityChangedEvent evt, CancellationToken ct)
    {
        var item = await _context.StockItems.FirstOrDefaultAsync(x => x.ProductId == evt.ProductId, ct);
        if (item == null)
        {
            item = new StockItemReadModel
            {
                ProductId = evt.ProductId,
                TotalQuantity = evt.TotalQuantity,
                ReservedQuantity = evt.ReservedQuantity,
                AvailableQuantity = evt.AvailableQuantity
            };
            _context.StockItems.Add(item);
        }
        else
        {
            item.TotalQuantity = evt.TotalQuantity;
            item.ReservedQuantity = evt.ReservedQuantity;
            item.AvailableQuantity = evt.AvailableQuantity;
            _context.StockItems.Update(item);
        }

        await _context.SaveChangesAsync(ct);
        await _cache.RemoveAsync(evt.ProductId, ct);
    }
}
