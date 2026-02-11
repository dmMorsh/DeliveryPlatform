using DeliveryService.Domain.Aggregates;

namespace DeliveryService.Application.Interfaces;

public interface IDeliveryRepository
{
    Task<Delivery?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Delivery?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default);
    Task<List<Delivery>> GetAssigningDeliveriesAsync(DateTime now, CancellationToken ct = default);
    Task<List<Guid>> GetTriedCourierIdsAsync(Guid deliveryId, CancellationToken ct = default);
    Task AddAsync(Delivery delivery, CancellationToken ct = default);
}
