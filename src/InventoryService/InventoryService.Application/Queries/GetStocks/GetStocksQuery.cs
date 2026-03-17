using InventoryService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace InventoryService.Application.Queries.GetStocks;

public record GetStocksQuery : IRequest<ApiResponse<List<StockItemView>>>;