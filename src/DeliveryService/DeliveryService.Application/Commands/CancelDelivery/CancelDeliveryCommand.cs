using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.CancelDelivery;

public record CancelDeliveryCommand(Guid DeliveryId, string? Reason) : IRequest<ApiResponse>;
