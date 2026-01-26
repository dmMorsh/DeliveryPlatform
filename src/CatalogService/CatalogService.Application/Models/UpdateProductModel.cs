namespace CatalogService.Application.Models;

public record UpdateProductModel(string? Name, string? Description, decimal? PriceCents, string? Currency, bool? IsActive);