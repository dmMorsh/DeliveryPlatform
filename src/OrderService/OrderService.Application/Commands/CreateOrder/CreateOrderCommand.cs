using MediatR;
using Shared.Utilities;

namespace OrderService.Application.Commands.CreateOrder;

public record CreateOrderCommand(CreateOrderModel CreateModel) : IRequest<ApiResponse<OrderView>>;
