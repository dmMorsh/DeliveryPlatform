using CatalogService.Domain.Aggregates;

namespace CatalogService.Application.Interfaces;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task<IEnumerable<Product>> SearchAsync(string? requestSearchTerm, CancellationToken ct = default);
}