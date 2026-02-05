using MediatR;
using OrderService.Application.Models;
using Shared.Utilities;

namespace OrderService.Application.Commands.UpdateReservedStock;

public record UpdateReservedStockCommand(Guid OrderId, UpdateOrderItemsModel OrderItemsModel ) : IRequest<ApiResponse>;