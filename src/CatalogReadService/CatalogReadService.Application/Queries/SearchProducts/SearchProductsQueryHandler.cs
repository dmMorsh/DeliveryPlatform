using CatalogReadService.Application.Common;
using CatalogReadService.Application.Interfaces;
using CatalogReadService.Application.Models;
using MediatR;
using Shared.Contracts;
using Shared.Utilities;

namespace CatalogReadService.Application.Queries.SearchProducts;

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
