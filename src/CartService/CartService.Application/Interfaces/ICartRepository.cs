using CartService.Domain.Aggregates;

namespace CartService.Application.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<Cart> CreateOrUpdateAsync(Cart cart, CancellationToken ct = default);
}
