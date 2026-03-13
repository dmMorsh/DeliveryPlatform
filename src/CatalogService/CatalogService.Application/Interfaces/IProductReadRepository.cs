using CatalogService.Application.Models;
using CatalogService.Application.Queries.SearchProducts;
using Shared.Contracts;

namespace CatalogService.Application.Interfaces;

public interface IProductReadRepository
{
    Task<PagedResult<ProductView>> SearchAsync(SearchProductsQuery query, CancellationToken ct);
    Task<ProductView?> GetByIdAsync(Guid id, CancellationToken ct);
}