using CatalogService.Domain.Aggregates;
using Shared.Contracts;

namespace CatalogService.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    // Task<PagedResult<Product>> SearchAsync(string? requestSearchTerm, bool? isActive, int page, int pageSize, CancellationToken ct = default);
}