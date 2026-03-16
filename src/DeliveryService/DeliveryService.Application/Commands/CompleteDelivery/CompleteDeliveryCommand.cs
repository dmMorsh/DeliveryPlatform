using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.CompleteDelivery;

public record CompleteDeliveryCommand(
    Guid DeliveryId,
    Guid CourierId,
    string? Signature,
    string? PhotoUrl,
    string? Notes,
    string? VerificationCode) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(
        DeliveryId,
        CourierId,
        Signature ?? string.Empty,
        PhotoUrl ?? string.Empty,
        Notes ?? string.Empty,
        VerificationCode ?? string.Empty);
}