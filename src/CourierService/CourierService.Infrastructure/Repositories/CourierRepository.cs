using CourierService.Domain.Aggregates;
using CourierService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CourierService.Repositories;

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
        _context.Couriers.Add(courier);
        // Do not save here - UnitOfWork will commit (keeps repository focused on persistence operations)
        return courier;
    }

    public async Task<Courier?> UpdateCourierAsync(Courier updatedCourier)
    {
        var courier = await GetCourierByIdAsync(updatedCourier.Id);
        if (courier == null)
            return null;

        // Assume domain methods were applied to the passed aggregate
        courier.Status = updatedCourier.Status;
        courier.CurrentLatitude = updatedCourier.CurrentLatitude ?? courier.CurrentLatitude;
        courier.CurrentLongitude = updatedCourier.CurrentLongitude ?? courier.CurrentLongitude;
        courier.LastLocationUpdate = updatedCourier.LastLocationUpdate ?? courier.LastLocationUpdate;
        courier.IsActive = updatedCourier.IsActive;
        courier.Rating = updatedCourier.Rating;
        courier.CompletedDeliveries = updatedCourier.CompletedDeliveries;
        courier.UpdatedAt = updatedCourier.UpdatedAt;

        _context.Couriers.Update(courier);
        // Do not call SaveChanges here; UnitOfWork will commit
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
