using InventoryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.AdjustStock;

public record AdjustStockCommand(SimpleStockItemModel[] Models) : IRequest<ApiResponse<List<ProcessedStockItemModel>?>>;