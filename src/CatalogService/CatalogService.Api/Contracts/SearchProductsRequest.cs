using CatalogService.Application.Common.Enums;
using CatalogService.Application.Queries;

namespace CatalogService.Api.Contracts;

public class SearchProductsRequest
{
    public string? Text { get; init; }
    public Guid? CategoryId { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }

    public ProductSortBy SortBy { get; init; } = ProductSortBy.Name;
    public SortDirection SortDir { get; init; } = SortDirection.Asc;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}