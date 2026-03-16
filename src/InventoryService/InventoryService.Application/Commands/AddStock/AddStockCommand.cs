using InventoryService.Application.Models;
using MediatR;
using Shared.Services;
using Shared.Utilities;

namespace InventoryService.Application.Commands.AddStock;

public record AddStockCommand(SimpleStockItemModel[] Models) : IRequest<ApiResponse<List<ProcessedStockItemModel>?>>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(
        Models.Length,
        string.Join(';', Models.OrderBy(m => m.ProductId)
            .Select(m => $"{m.ProductId:N}:{m.Quantity}")));
}