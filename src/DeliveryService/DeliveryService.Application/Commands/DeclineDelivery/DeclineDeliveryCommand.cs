using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.DeclineDelivery;

public record DeclineDeliveryCommand(Guid DeliveryId, Guid CourierId, string? Reason) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(
        DeliveryId,
        CourierId,
        Reason ?? string.Empty);
}