using InventoryService.Application.Models;
using MediatR;
using Shared.Contracts;
using Shared.Services;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReleaseStock;

public record ReleaseStockCommand(Guid OrderId, SimpleStockItemModel[] ReleaseStockModels) : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(
        OrderId,
        ReleaseStockModels.Length,
        string.Join(';', ReleaseStockModels.OrderBy(m => m.ProductId)
            .Select(m => $"{m.ProductId:N}:{m.Quantity}")));
}