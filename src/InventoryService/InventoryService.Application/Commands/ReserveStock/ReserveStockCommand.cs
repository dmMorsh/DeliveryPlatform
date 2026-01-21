using InventoryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReserveStock;

public record ReserveStockCommand(Guid ProductId, Guid OrderId, int Quantity) : IRequest<ApiResponse<StockView>>;