using MediatR;
using Shared.Contracts;
using Shared.Services;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.FailDelivery;

public record FailDeliveryCommand(Guid DeliveryId, string? Reason) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(DeliveryId, Reason ?? string.Empty);
}