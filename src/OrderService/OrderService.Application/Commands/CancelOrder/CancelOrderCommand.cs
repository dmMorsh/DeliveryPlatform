using MediatR;
using Shared.Contracts;
using Shared.Services;
using Shared.Utilities;

namespace OrderService.Application.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId, string? Reason) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(OrderId, Reason ?? string.Empty);
}