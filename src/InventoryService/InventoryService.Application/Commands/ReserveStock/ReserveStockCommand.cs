using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReserveStock;

public record ReserveStockCommand(Guid OrderId, ReserveStockModel[] ReserveStockModels, int ShardId) 
    : IRequest<ApiResponse<List<StockView>>>, IHangfireRetryable
{
    public Guid CorrelationId => OrderId;
}