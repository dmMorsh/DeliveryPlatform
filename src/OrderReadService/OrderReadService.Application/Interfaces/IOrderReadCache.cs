using OrderReadService.Domain.Models;

namespace OrderReadService.Application.Interfaces;

public interface IOrderReadCache
{
    Task<OrderReadModel?> GetAsync(Guid orderId, CancellationToken ct);
    Task SetAsync(OrderReadModel view, CancellationToken ct);
    Task RemoveAsync(Guid orderId, CancellationToken ct);
}
