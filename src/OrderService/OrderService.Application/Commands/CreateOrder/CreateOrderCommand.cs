using MediatR;
using OrderService.Application.Models;
using Shared.Utilities;

namespace OrderService.Application.Commands.CreateOrder;

public record CreateOrderCommand(
    Guid ClientId,
    string FromAddress,
    string ToAddress,
    double FromLatitude,
    double FromLongitude,
    double ToLatitude,
    double ToLongitude,
    string Description,
    int WeightGrams,
    long CostCents,
    string Currency,
    string? CourierNote,
    IReadOnlyCollection<CreateOrderItemDto>? Items,
    Guid? CheckoutId,
    DateTime? DesiredReadyAt
) : IRequest<ApiResponse<OrderView>>;

public record CreateOrderItemDto(Guid ProductId, string Name, int PriceCents, int Quantity);
