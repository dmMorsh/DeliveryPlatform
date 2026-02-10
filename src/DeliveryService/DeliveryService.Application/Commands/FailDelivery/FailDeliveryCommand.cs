using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.FailDelivery;

public record FailDeliveryCommand(Guid DeliveryId, string? Reason) : IRequest<ApiResponse>;
