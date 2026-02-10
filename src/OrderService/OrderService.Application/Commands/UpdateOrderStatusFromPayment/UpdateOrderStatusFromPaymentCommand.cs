using MediatR;
using OrderService.Domain.Aggregates;
using Shared.Utilities;

namespace OrderService.Application.Commands.UpdateOrderStatusFromPayment;

public record UpdateOrderStatusFromPaymentCommand(
    Guid OrderId,
    OrderStatus NewStatus,
    string Reason) : IRequest<ApiResponse>;
