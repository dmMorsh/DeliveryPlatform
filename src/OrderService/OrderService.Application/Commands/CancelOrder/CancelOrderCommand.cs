using MediatR;
using Shared.Utilities;

namespace OrderService.Application.Commands.CancelOrder;

public record CancelOrderCommand(Guid OrderId, string? Reason) : IRequest<ApiResponse>;
