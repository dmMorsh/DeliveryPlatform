using CatalogService.Application.Models;
using MediatR;
using Shared.Contracts;

namespace CatalogService.Application.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ApiResponse<ProductView>>;