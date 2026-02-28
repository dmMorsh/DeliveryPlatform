using CourierService.Application.Interfaces;
using CourierService.Domain.Aggregates;
using CourierService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourierService.Infrastructure.Repositories;

public class CourierRepository : ICourierRepository
{
    private readonly CourierDbContext _context;

    public CourierRepository(CourierDbContext context)
    {
        _context = context;
    }

    public async Task<Courier?> GetCourierByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Couriers.FindAsync([id], ct);
    }

    public async Task<Courier?> GetCourierByPhoneAsync(string phone, CancellationToken ct)
    {
        return await _context.Couriers.FirstOrDefaultAsync(c => c.Phone == phone, ct);
    }

    public async Task<List<Courier>> GetActiveCouriersAsync(CancellationToken ct)
    {
        return await _context.Couriers
            .Where(c => c.IsActive && c.Status == CourierStatus.Online)
            .OrderByDescending(c => c.Rating)
            .ToListAsync(ct);
    }

    public async Task<Courier> CreateCourierAsync(Courier courier, CancellationToken ct)
    {
        await _context.Couriers.AddAsync(courier, ct);
        return courier;
    }
    
    public async Task<(List<Courier> Items, int Total)> GetCouriersPagedAsync(CancellationToken ct, int page = 1, int pageSize = 20)
    {
        var query = _context.Couriers.OrderByDescending(c => c.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
