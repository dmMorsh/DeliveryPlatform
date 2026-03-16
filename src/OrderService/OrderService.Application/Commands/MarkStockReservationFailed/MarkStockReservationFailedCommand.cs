using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkStockReservationFailed;

public record MarkStockReservationFailedCommand(
    Guid OrderId,
    IReadOnlyCollection<MarkStockFailedItemDto> Items,
    string? Description = null
) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(
        OrderId,
        Items.Count,
        Description ?? string.Empty,
        string.Join(';', Items.OrderBy(i => i.ProductId)
            .Select(i => $"{i.ProductId:N}:{i.Quantity}:{i.Description ?? string.Empty}")));
}

public record MarkStockFailedItemDto(Guid ProductId, int Quantity, string? Description = null);