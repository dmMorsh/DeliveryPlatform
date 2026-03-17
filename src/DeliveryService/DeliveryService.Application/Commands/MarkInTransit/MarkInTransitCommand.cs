using MediatR;
using Shared.Contracts;
using Shared.Services;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.MarkInTransit;

public record MarkInTransitCommand(Guid DeliveryId, Guid CourierId) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(DeliveryId, CourierId);
}