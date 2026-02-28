using InventoryService.Application.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly InventoryDbContext _context;

    public ReservationRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task AddReservationAsync(StockReservation item, CancellationToken ct)
    {
        await _context.AddAsync(item, ct);
    }
    
    public async Task<bool> ReservationExistAsync(Guid orderId, Guid productId, CancellationToken ct)
    {
        return await _context.StockReservation.AnyAsync(sr=> sr.OrderId == orderId && sr.ProductId == productId, ct);
    }

    public async Task<List<Guid>> GetReservedProductIdsAsync(Guid orderId, IEnumerable<Guid> productIds, CancellationToken ct)
    {
        var ids = productIds.ToList();
        if (ids.Count == 0)
            return new List<Guid>();

        return await _context.StockReservation
            .Where(sr => sr.OrderId == orderId && ids.Contains(sr.ProductId))
            .Select(sr => sr.ProductId)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<List<StockReservation>> GetActiveReservationsAsync(Guid orderId, CancellationToken ct)
    {
        return await _context.StockReservation
            .Where(sr => sr.OrderId == orderId && sr.ReleasedAt == null)
            .ToListAsync(cancellationToken: ct);
    }

    public async Task<List<Guid>> GetStaleOrderIdsAsync(DateTime cutoffUtc, int batchSize, CancellationToken ct)
    {
        return await _context.StockReservation
            .Where(sr => sr.ReleasedAt == null && sr.CreatedAt < cutoffUtc)
            .OrderBy(sr => sr.CreatedAt)
            .Select(sr => sr.OrderId)
            .Distinct()
            .Take(batchSize)
            .ToListAsync(ct);
    }
}
