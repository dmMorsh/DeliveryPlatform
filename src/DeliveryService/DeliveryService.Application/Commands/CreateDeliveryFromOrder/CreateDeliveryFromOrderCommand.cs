using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.CreateDeliveryFromOrder;

public record CreateDeliveryFromOrderCommand(
    Guid OrderId,
    Guid ClientId,
    string FromAddress,
    string ToAddress,
    double FromLatitude,
    double FromLongitude,
    double ToLatitude,
    double ToLongitude) : IRequest<ApiResponse>;
