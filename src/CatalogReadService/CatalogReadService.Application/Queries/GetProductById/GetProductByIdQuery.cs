using CatalogReadService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace CatalogReadService.Application.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ApiResponse<ProductView>>;
