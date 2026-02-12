using CatalogService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CatalogService.Application.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string? Description,
    long PriceCents,
    string? Currency,
    long WeightGrams) : IRequest<ApiResponse<ProductView>>;
