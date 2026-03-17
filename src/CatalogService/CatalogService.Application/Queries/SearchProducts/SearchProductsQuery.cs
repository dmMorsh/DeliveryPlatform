using CatalogService.Application.Common.Enums;
using CatalogService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace CatalogService.Application.Queries.SearchProducts;

public record SearchProductsQuery(
    string? Text,
    Guid? CategoryId,
    long? MinPrice,
    long? MaxPrice,
    ProductSortBy SortBy,
    SortDirection SortDirection,
    int Page,
    int PageSize
) : IRequest<ApiResponse<PagedResult<ProductView>>>;