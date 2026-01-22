using MediatR;
using CatalogService.Application.Models;
using Shared.Utilities;

namespace CatalogService.Application.Commands.CreateProduct;

public record CreateProductCommand(CreateProductModel Model) : IRequest<ApiResponse<ProductView>>;
