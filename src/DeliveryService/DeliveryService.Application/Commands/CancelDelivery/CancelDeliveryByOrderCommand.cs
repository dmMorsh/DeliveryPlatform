using MediatR;
using Shared.Contracts;
using Shared.Services;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.CancelDelivery;

public record CancelDeliveryByOrderCommand(Guid OrderId, string? Reason) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(OrderId, Reason ?? string.Empty);
}