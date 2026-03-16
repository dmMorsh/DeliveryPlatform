using CatalogReadService.Application.Interfaces;
using CatalogReadService.Application.Models;
using MediatR;
using Shared.Contracts;
using Shared.Services;
using Shared.Utilities;

namespace CatalogReadService.Application.Queries.SearchProducts;

public class SearchProductsQueryHandler
    : IRequestHandler<SearchProductsQuery, ApiResponse<PagedResult<ProductView>>>
{
    private static readonly SingleFlight<string, PagedResult<ProductView>> SingleFlight = new();
    private readonly IProductReadRepository _readRepo;

    public SearchProductsQueryHandler(IProductReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<ApiResponse<PagedResult<ProductView>>> Handle(
        SearchProductsQuery request,
        CancellationToken ct)
    {
        var requestHash = request.GetRequestHash();
        var task = SingleFlight.RunAsync(
            requestHash,
            token => _readRepo.SearchAsync(requestHash, request, token));
        var result = await task.WaitAsync(ct);
        return ApiResponse<PagedResult<ProductView>>.SuccessResponse(result);
    }
}
