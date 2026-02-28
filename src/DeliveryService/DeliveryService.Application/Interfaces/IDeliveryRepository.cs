using DeliveryService.Domain.Aggregates;

namespace DeliveryService.Application.Interfaces;

public interface IDeliveryRepository
{
    Task<Delivery?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken ct);
    Task<List<Delivery>> GetAssigningDeliveriesAsync(DateTime now, CancellationToken ct);
    Task<Delivery?> GetActiveOfferByCourierIdAsync(Guid courierId, DateTime now, CancellationToken ct);
    Task<List<Delivery>> GetActiveDeliveriesByCourierIdsAsync(IReadOnlyCollection<Guid> courierIds, CancellationToken ct);
    Task<List<Guid>> GetTriedCourierIdsAsync(Guid deliveryId, CancellationToken ct);
    Task AddAsync(Delivery delivery, CancellationToken ct);
}
