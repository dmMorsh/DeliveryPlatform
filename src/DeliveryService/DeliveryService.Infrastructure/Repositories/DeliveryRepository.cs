using DeliveryService.Application.Interfaces;
using DeliveryService.Domain.Aggregates;
using DeliveryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeliveryService.Infrastructure.Repositories;

public class DeliveryRepository : IDeliveryRepository
{
    private readonly DeliveryDbContext _db;

    public DeliveryRepository(DeliveryDbContext db)
    {
        _db = db;
    }

    public async Task<Delivery?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Deliveries
            .Include(d => d.AssignmentAttempts)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
    {
        return await _db.Deliveries
            .Include(d => d.AssignmentAttempts)
            .FirstOrDefaultAsync(d => d.OrderId == orderId, ct);
    }

    public async Task<List<Delivery>> GetAssigningDeliveriesAsync(DateTime now, CancellationToken ct = default)
    {
        return await _db.Deliveries
            .Include(d => d.AssignmentAttempts)
            .Where(d => d.Status == DeliveryStatus.Assigning &&
                        (d.CurrentOfferExpiresAt == null || d.CurrentOfferExpiresAt <= now))
            .TagWith("INFRA_BACKGROUND_POLL")
            .ToListAsync(ct);
    }

    public async Task<Delivery?> GetActiveOfferByCourierIdAsync(Guid courierId, DateTime now, CancellationToken ct = default)
    {
        return await _db.Deliveries
            .AsNoTracking()
            .Where(d => d.Status == DeliveryStatus.Assigning
                        && d.CurrentOfferCourierId == courierId
                        && d.CurrentOfferExpiresAt != null
                        && d.CurrentOfferExpiresAt > now)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<Delivery>> GetActiveDeliveriesByCourierIdsAsync(IReadOnlyCollection<Guid> courierIds, CancellationToken ct = default)
    {
        if (courierIds.Count == 0)
            return [];

        return await _db.Deliveries
            .AsNoTracking()
            .Where(d => d.CourierId != null
                        && courierIds.Contains(d.CourierId.Value)
                        && (d.Status == DeliveryStatus.Assigned
                            || d.Status == DeliveryStatus.PickedUp
                            || d.Status == DeliveryStatus.InDelivery))
            .ToListAsync(ct);
    }

    public Task<List<Guid>> GetTriedCourierIdsAsync(Guid deliveryId, CancellationToken ct = default)
    {
        return _db.Deliveries
            .Where(d => d.Id == deliveryId)
            .SelectMany(d => d.AssignmentAttempts)
            .Select(a => a.CourierId)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Delivery delivery, CancellationToken ct = default)
    {
        await _db.Deliveries.AddAsync(delivery, ct);
    }
}
