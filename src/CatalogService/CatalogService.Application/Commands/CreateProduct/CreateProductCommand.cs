using CatalogService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CatalogService.Application.Commands.CreateProduct;

public record CreateProductCommand(CreateProductModel Model) : IRequest<ApiResponse<ProductView>>;
