using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.MarkPickedUp;

public record MarkPickedUpCommand(Guid DeliveryId, Guid CourierId) : IRequest<ApiResponse>;
