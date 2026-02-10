using MediatR;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.CancelDelivery;

public record CancelDeliveryByOrderCommand(Guid OrderId, string? Reason) : IRequest<ApiResponse>;
