using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.DeclineDelivery;

public record DeclineDeliveryCommand(Guid DeliveryId, Guid CourierId, string? Reason) : IRequest<ApiResponse>;
