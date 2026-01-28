using CartService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CartService.Application.Commands.Checkout;

public record CheckoutCartCommand(Guid CustomerId, CheckoutCartModel Model) : IRequest<ApiResponse<string>>;
