using InventoryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Commands.ReleaseStock;

public record ReleaseStockCommand(Guid OrderId, SimpleStockItemModel[] ReleaseStockModels) : IRequest<ApiResponse>;