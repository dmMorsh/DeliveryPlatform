using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.ReturnDelivery;

public record ReturnDeliveryCommand(Guid DeliveryId, string? Reason) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(DeliveryId, Reason ?? string.Empty);
}