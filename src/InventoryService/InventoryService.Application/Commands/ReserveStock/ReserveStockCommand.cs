using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReserveStock;

public record ReserveStockCommand(Guid OrderId, SimpleStockItemModel[] ReserveStockModels) 
    : IRequest<ApiResponse>, IHangfireRetryable
{
    public Guid CorrelationId => OrderId;
}