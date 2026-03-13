using CatalogReadService.Application.Common;
using CatalogReadService.Application.Common.Enums;
using CatalogReadService.Application.Models;
using MediatR;
using Shared.Contracts;
using Shared.Utilities;

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
) : IRequest<ApiResponse<PagedResult<ProductView>>>;
