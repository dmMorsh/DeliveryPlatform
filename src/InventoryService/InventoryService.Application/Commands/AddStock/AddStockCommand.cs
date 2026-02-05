using InventoryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.AddStock;

public record AddStockCommand(SimpleStockItemModel[] Models) : IRequest<ApiResponse<List<ProcessedStockItemModel>?>>;