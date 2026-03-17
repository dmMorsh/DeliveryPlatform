using MediatR;
using OrderService.Domain.Aggregates;
using Shared.Contracts;
using Shared.Services;
using Shared.Utilities;

namespace OrderService.Application.Commands.UpdateOrderStatusFromPayment;

public record UpdateOrderStatusFromPaymentCommand(
    Guid OrderId,
    OrderStatus NewStatus,
    string Reason) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(OrderId, NewStatus, Reason ?? string.Empty);
}