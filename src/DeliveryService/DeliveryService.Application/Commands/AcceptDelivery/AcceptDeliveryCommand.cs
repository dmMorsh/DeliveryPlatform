using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.AcceptDelivery;

public record AcceptDeliveryCommand(Guid DeliveryId, Guid CourierId) : IRequest<ApiResponse>;
