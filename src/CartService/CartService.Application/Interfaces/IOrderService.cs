using CartService.Application.Models;
using CartService.Domain.Aggregates;

namespace CartService.Application.Interfaces;

public interface IOrderService
{
    Task<Guid> CreateOrderFromCartAsync(Cart cart, CheckoutCartModel model, CancellationToken ct);
}
