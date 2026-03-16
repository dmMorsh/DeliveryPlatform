using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkOrderAccepted;

public record MarkOrderAcceptedCommand(Guid OrderId) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(OrderId, "accepted");
}