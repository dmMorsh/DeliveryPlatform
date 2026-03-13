using CartService.Application.Models;

namespace CartService.Application.Interfaces;

public interface ICartReadCache
{
    Task<CartView?> GetAsync(Guid customerId, CancellationToken ct = default);
    Task SetAsync(Guid customerId, CartView cart, CancellationToken ct = default);
    Task InvalidateAsync(Guid customerId, CancellationToken ct = default);
}
