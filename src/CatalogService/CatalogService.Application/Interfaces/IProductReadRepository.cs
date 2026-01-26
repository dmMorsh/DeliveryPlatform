using CatalogService.Application.Common;
using CatalogService.Application.Models;
using CatalogService.Application.Queries.SearchProducts;

namespace CatalogService.Application.Interfaces;

public interface IProductReadRepository
{
    Task<PagedResult<ProductView>> SearchAsync(SearchProductsQuery query, CancellationToken ct);
    Task<ProductView?> GetByIdAsync(Guid id, CancellationToken ct);
}