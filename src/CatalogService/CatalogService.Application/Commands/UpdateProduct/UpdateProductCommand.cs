using CatalogService.Application.Models;
using MediatR;
using Shared.Contracts;
using Shared.Services;
using Shared.Utilities;

namespace CatalogService.Application.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid ProductId, 
    string? Name,
    string? Description,
    long? PriceCents,
    string? Currency,
    bool? IsActive) 
    : IRequest<ApiResponse<ProductView>>, IHangfireRetryable
{
    public Guid CorrelationId => DeterministicGuid.FromComponents(
        ProductId,
        Name ?? string.Empty,
        Description ?? string.Empty,
        PriceCents,
        Currency ?? string.Empty,
        IsActive);
}