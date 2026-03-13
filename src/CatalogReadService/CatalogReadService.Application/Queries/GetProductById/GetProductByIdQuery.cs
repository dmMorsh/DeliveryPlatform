using CatalogReadService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CatalogReadService.Application.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ApiResponse<ProductView>>;
