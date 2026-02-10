using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.CompleteDelivery;

public record CompleteDeliveryCommand(
    Guid DeliveryId,
    Guid CourierId,
    string? Signature,
    string? PhotoUrl,
    string? Notes,
    string? VerificationCode) : IRequest<ApiResponse>;
