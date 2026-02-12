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

    public async Task<Courier?> GetCourierByIdAsync(Guid id)
    {
        return await _context.Couriers.FindAsync(id);
    }

    public async Task<Courier?> GetCourierByPhoneAsync(string phone)
    {
        return await _context.Couriers.FirstOrDefaultAsync(c => c.Phone == phone);
    }

    public async Task<List<Courier>> GetActiveCouriersAsync()
    {
        return await _context.Couriers
            .Where(c => c.IsActive && c.Status == CourierStatus.Online)
            .OrderByDescending(c => c.Rating)
            .ToListAsync();
    }

    public async Task<Courier> CreateCourierAsync(Courier courier)
    {
        await _context.Couriers.AddAsync(courier);
        return courier;
    }
    
    public async Task<(List<Courier> Items, int Total)> GetCouriersPagedAsync(int page = 1, int pageSize = 20)
    {
        var query = _context.Couriers.OrderByDescending(c => c.CreatedAt);
        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}
