using CatalogService.Application.Common;
using CatalogService.Application.Common.Enums;
using CatalogService.Application.Interfaces;
using CatalogService.Application.Models;
using CatalogService.Application.Queries;
using CatalogService.Application.Queries.SearchProducts;
using CatalogService.Domain.Aggregates;
using CatalogService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Repositories;

public class ProductReadRepository : IProductReadRepository
{
    private readonly CatalogDbContext _db;

    public ProductReadRepository(CatalogDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ProductView>> SearchAsync(SearchProductsQuery request, CancellationToken ct)
    {
        IQueryable<Product> query = _db.Products;

        // 🔍 Text search
        if (!string.IsNullOrWhiteSpace(request.Text))
        {
            var text = request.Text.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(text) ||
                (p.Description != null && p.Description.ToLower().Contains(text))
                );
        }

        // 📦 Category
        // if (request.CategoryId.HasValue)
        // {
        //     query = query.Where(p => p.CategoryId == request.CategoryId);
        // }

        // 💰 Price
        if (request.MinPrice.HasValue)
            query = query.Where(p => p.Price.Amount >= request.MinPrice);

        if (request.MaxPrice.HasValue)
            query = query.Where(p => p.Price.Amount <= request.MaxPrice);

        // 📊 Sorting
        query = request.SortBy switch
        {
            ProductSortBy.Price => request.SortDirection == SortDirection.Asc
                ? query.OrderBy(p => p.Price.Amount)
                : query.OrderByDescending(p => p.Price.Amount),

            ProductSortBy.CreatedAt => request.SortDirection == SortDirection.Asc
                ? query.OrderBy(p => p.CreatedAt)
                : query.OrderByDescending(p => p.CreatedAt),

            _ => request.SortDirection == SortDirection.Asc
                ? query.OrderBy(p => p.Name)
                : query.OrderByDescending(p => p.Name)
        };

        var total = await query.CountAsync(ct);

        var items = await query
            .AsNoTracking()
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductView(
                p.Id,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Price.Currency))
            .ToListAsync(ct);

        return new PagedResult<ProductView>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<ProductView?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => 
                new ProductView(p.Id, p.Name, p.Description, p.Price.Amount, p.Price.Currency)
            )
            .FirstOrDefaultAsync(ct);
    }
}