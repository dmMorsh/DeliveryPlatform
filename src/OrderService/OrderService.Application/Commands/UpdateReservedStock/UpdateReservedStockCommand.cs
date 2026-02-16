using MediatR;
using Shared.Utilities;

namespace OrderService.Application.Commands.UpdateReservedStock;

public record UpdateReservedStockCommand(
    Guid OrderId,
    IReadOnlyCollection<UpdateOrderItemDto> Items,
    string? Description = null
) : IRequest<ApiResponse>;

public record UpdateOrderItemDto(Guid ProductId, int Quantity, string? Description = null);