using CatalogService.Application.Interfaces;
using CatalogService.Domain.Aggregates;
using CatalogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly CatalogDbContext _context;

    public ProductRepository(CatalogDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await _context.Products.AddAsync(product, ct);
    }

    public Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        var existing = _context.Products.FirstOrDefault(x => x.Id == product.Id);
        if (existing != null)
        {
            _context.Entry(existing).CurrentValues.SetValues(product);
        }
        return Task.CompletedTask;
    }

    public Task<List<Product>> SearchAsync(string? requestSearchTerm, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
