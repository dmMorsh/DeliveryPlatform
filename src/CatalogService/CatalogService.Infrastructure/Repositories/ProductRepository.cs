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

    // public async Task<PagedResult<Product>> SearchAsync(
    //     string? requestSearchTerm,
    //     bool? isActive,
    //     int page,
    //     int pageSize,
    //     CancellationToken ct = default)
    // {
    //     var query = _context.Products.AsNoTracking();
    //
    //     if (!string.IsNullOrWhiteSpace(requestSearchTerm))
    //     {
    //         var term = requestSearchTerm.Trim();
    //         query = query.Where(p =>
    //             p.Name.Contains(term) ||
    //             (p.Description != null && p.Description.Contains(term)));
    //     }
    //
    //     if (isActive.HasValue)
    //         query = query.Where(p => p.IsActive == isActive.Value);
    //
    //     var total = await query.CountAsync(ct);
    //
    //     var items = await query
    //         .OrderByDescending(p => p.CreatedAt)
    //         .Skip((page - 1) * pageSize)
    //         .Take(pageSize)
    //         .ToListAsync(ct);
    //
    //     return new PagedResult<Product>
    //     {
    //         Items = items,
    //         TotalCount = total,
    //         Page = page,
    //         PageSize = pageSize
    //     };
    // }
}
