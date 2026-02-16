using MediatR;
using OrderService.Application.Models;
using OrderService.Domain.Aggregates;
using Shared.Utilities;

namespace OrderService.Application.Commands.UpdateOrder;

public record UpdateOrderCommand(
    Guid OrderId,
    Guid? CourierId,
    string? CourierName,
    OrderStatus? Status,
    string? CourierNote
) : IRequest<ApiResponse<OrderView>>;