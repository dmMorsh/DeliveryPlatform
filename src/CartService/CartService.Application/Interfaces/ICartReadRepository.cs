using CartService.Application.Models;

namespace CartService.Application.Interfaces;

public interface ICartReadRepository
{
    Task<CartView?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
}
