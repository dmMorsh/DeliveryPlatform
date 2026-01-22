using MediatR;
using CatalogService.Application.Models;
using Shared.Utilities;

namespace CatalogService.Application.Commands.UpdateProduct;

public record UpdateProductCommand(Guid ProductId, string? Name, string? Description, long? PriceCents, int? StockQuantity) 
    : IRequest<ApiResponse<ProductView>>;
