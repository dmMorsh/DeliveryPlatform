using CartService.Application.Commands.Checkout;
using CartService.Domain.Aggregates;

namespace CartService.Application.Interfaces;

public interface IOrderService
{
    Task<Guid> CreateOrderFromCartAsync(Cart cart, CheckoutCartCommand command, CancellationToken ct);
}
