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
    
    public async Task<bool> ReservationExistAsync(Guid orderId, CancellationToken ct)
    {
        return await _context.StockReservation.AnyAsync(sr=> sr.OrderId == orderId, ct);
    }

    public async Task<List<StockReservation>> GetActiveReservationsAsync(Guid orderId, CancellationToken ct)
    {
        return await _context.StockReservation
            .Where(sr => sr.OrderId == orderId && sr.ReleasedAt == null)
            .ToListAsync(cancellationToken: ct);
    }
}
