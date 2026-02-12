using CatalogService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CatalogService.Application.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid ProductId, 
    string? Name,
    string? Description,
    long? PriceCents,
    string? Currency,
    bool? IsActive) 
    : IRequest<ApiResponse<ProductView>>;
