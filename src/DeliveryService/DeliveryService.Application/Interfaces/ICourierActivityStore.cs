namespace DeliveryService.Application.Interfaces;

public interface ICourierActivityStore
{
    Task TouchAsync(Guid courierId, DateTime now, CancellationToken ct = default);
    Task<bool> IsActiveAsync(Guid courierId, DateTime now, CancellationToken ct = default);
}
