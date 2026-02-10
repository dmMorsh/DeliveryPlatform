using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.MarkInTransit;

public record MarkInTransitCommand(Guid DeliveryId, Guid CourierId) : IRequest<ApiResponse>;
