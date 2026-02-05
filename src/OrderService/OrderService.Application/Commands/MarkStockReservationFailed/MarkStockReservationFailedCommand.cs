using MediatR;
using OrderService.Application.Models;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkStockReservationFailed;

public record MarkStockReservationFailedCommand(Guid OrderId, UpdateOrderItemsModel OrderItemsModel) : IRequest<ApiResponse>;