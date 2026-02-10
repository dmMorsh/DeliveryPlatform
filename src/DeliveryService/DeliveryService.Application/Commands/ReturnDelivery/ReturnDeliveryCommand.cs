using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.ReturnDelivery;

public record ReturnDeliveryCommand(Guid DeliveryId, string? Reason) : IRequest<ApiResponse>;
