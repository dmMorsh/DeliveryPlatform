using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkOrderRejected;

public record MarkOrderRejectedCommand(Guid OrderId, string? Reason) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(OrderId, Reason ?? string.Empty, "rejected");
}