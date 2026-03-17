using InventoryService.Application.Models;
using InventoryService.Application.Read;
using MediatR;
using Shared.Contracts;

namespace InventoryService.Application.Queries.GetStocks;

public class GetStocksQueryHandler : IRequestHandler<GetStocksQuery, ApiResponse<List<StockItemView>>>
{
    private readonly IInventoryReadRepository _readRepo;

    public GetStocksQueryHandler(IInventoryReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<ApiResponse<List<StockItemView>>> Handle(GetStocksQuery request, CancellationToken ct)
    {
        var itemViews = await _readRepo.GetAllAsync(ct);
        return ApiResponse<List<StockItemView>>.SuccessResponse(itemViews);
    }
}