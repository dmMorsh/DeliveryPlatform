using CatalogReadService.Application.Models;
using CatalogReadService.Application.Queries.SearchProducts;
using Shared.Contracts;

namespace CatalogReadService.Application.Interfaces;

public interface IProductReadRepository
{
    Task<PagedResult<ProductView>> SearchAsync(string requestHash, SearchProductsQuery query, CancellationToken ct);
    Task<ProductView?> GetByIdAsync(Guid id, CancellationToken ct);
}
