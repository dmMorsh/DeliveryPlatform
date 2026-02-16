using MediatR;
using Shared.Utilities;

namespace CartService.Application.Commands.Checkout;

public record CheckoutCartCommand(
    Guid CustomerId,
    string FromAddress,
    string ToAddress,
    double FromLatitude,
    double FromLongitude,
    double ToLatitude,
    double ToLongitude,
    int WeightGrams,
    long CostCents,
    string? Currency,
    string? CourierNote
) : IRequest<ApiResponse<Guid>>;
