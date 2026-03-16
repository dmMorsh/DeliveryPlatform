using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkOrderReady;

public record MarkOrderReadyCommand(Guid OrderId) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(OrderId, "ready");
}