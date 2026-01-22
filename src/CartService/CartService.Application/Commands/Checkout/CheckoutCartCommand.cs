using MediatR;
using Shared.Utilities;

namespace CartService.Application.Commands.Checkout;

public record CheckoutCartCommand(Guid CustomerId) : IRequest<ApiResponse<string>>;
