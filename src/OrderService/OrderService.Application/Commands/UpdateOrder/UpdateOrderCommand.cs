using MediatR;
using Shared.Utilities;

namespace OrderService.Application.Commands.UpdateOrder;

public record UpdateOrderCommand(Guid OrderId, UpdateOrderModel Model ) : IRequest<ApiResponse<OrderView>>;