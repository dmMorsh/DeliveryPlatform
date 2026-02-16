using MediatR;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkStockReservationFailed;

public record MarkStockReservationFailedCommand(
    Guid OrderId,
    IReadOnlyCollection<MarkStockFailedItemDto> Items,
    string? Description = null
) : IRequest<ApiResponse>;

public record MarkStockFailedItemDto(Guid ProductId, int Quantity, string? Description = null);