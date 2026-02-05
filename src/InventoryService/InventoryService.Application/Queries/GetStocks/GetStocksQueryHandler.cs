using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace InventoryService.Application.Queries.GetStocks;

public class GetStocksQueryHandler : IRequestHandler<GetStocksQuery, ApiResponse<List<StockItemView>>>
{
    private readonly IUnitOfWorkFactory _factory;

    public GetStocksQueryHandler(IUnitOfWorkFactory factory)
    {
        _factory = factory;
    }

    public async Task<ApiResponse<List<StockItemView>>> Handle(GetStocksQuery request, CancellationToken ct)
    {
        await using var uow = _factory.Create(0);
        var itemViews = uow.Stock
            .GetAllProductAsync(ct).Result
            .Select(s=> new StockItemView
            {
                ProductId = s.Id,
                TotalQuantity = s.TotalQuantity,
                ReservedQuantity = s.ReservedQuantity,
                AvailableQuantity = s.AvailableQuantity,
            }).ToList(); // TODO переводить на мапстер
        
        return ApiResponse<List<StockItemView>>.SuccessResponse(itemViews);
    }
}