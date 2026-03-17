using System.Security.Cryptography;
using System.Text;
using CatalogReadService.Application.Common.Enums;
using CatalogReadService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace CatalogReadService.Application.Queries.SearchProducts;

public record SearchProductsQuery(
    string? Text,
    Guid? CategoryId,
    long? MinPrice,
    long? MaxPrice,
    ProductSortBy SortBy,
    SortDirection SortDirection,
    int Page,
    int PageSize
) : IRequest<ApiResponse<PagedResult<ProductView>>>
{
    public string GetRequestHash()
    {
        var normalized = string.Join("|", new[]
        {
            (Text ?? string.Empty).Trim(),
            CategoryId?.ToString() ?? string.Empty,
            MinPrice?.ToString() ?? string.Empty,
            MaxPrice?.ToString() ?? string.Empty,
            SortBy.ToString(),
            SortDirection.ToString(),
            Page.ToString(),
            PageSize.ToString()
        });

        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        var hash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return hash;
    }
}