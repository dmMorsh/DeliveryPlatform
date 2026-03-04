using DeliveryService.Application.Models;

namespace DeliveryService.Application.Interfaces;

public interface IDeliveryOfferCache
{
    Task<CourierOfferView?> GetAsync(Guid courierId, CancellationToken ct);
    Task SetAsync(Guid courierId, CourierOfferView view, CancellationToken ct);
    Task RemoveAsync(Guid courierId, CancellationToken ct);
}
