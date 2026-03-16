using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.AcceptDelivery;

public record AcceptDeliveryCommand(Guid DeliveryId, Guid CourierId) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(DeliveryId, CourierId);
}