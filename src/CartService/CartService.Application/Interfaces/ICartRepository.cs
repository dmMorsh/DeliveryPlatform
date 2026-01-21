using CartService.Domain.Aggregates;

namespace CartService.Application.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetCartByCustomerIdAsync(Guid customerId);
    Task<Cart> CreateOrUpdateAsync(Cart cart);
}
