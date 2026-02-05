using CatalogService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CatalogService.Application.Commands.UpdateProduct;

public record UpdateProductCommand(Guid ProductId, UpdateProductModel  UpdateProductModel) 
    : IRequest<ApiResponse<ProductView>>;
