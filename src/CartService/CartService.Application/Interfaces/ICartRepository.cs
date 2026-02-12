using CartService.Domain.Aggregates;

namespace CartService.Application.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetCartByCustomerIdAsync(Guid customerId, CancellationToken ct);
    Task AddAsync(Cart cart, CancellationToken ct);
}
