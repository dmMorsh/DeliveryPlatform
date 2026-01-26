using CatalogService.Application.Common;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CatalogService.Application.Queries.SearchProducts;

public class SearchProductsQueryHandler
    : IRequestHandler<SearchProductsQuery, ApiResponse<PagedResult<ProductView>>>
{
    private readonly IProductReadRepository _readRepo;

    public SearchProductsQueryHandler(IProductReadRepository readRepo)
    {
        _readRepo = readRepo;
    }

    public async Task<ApiResponse<PagedResult<ProductView>>> Handle(
        SearchProductsQuery request,
        CancellationToken ct)
    {
        var result = await _readRepo.SearchAsync(request, ct);
        return ApiResponse<PagedResult<ProductView>>.SuccessResponse(result);
    }
}
