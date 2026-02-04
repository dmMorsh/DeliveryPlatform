using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReserveStock;

public record ReserveStockCommand(Guid OrderId, ReserveStockModel[] ReserveStockModels) 
    : IRequest<ApiResponse<Unit>>, IHangfireRetryable
{
    public Guid CorrelationId => OrderId;
}