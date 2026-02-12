namespace CatalogService.Application.Models;

public record UpdateProductRequest(string? Name, string? Description, long? PriceCents, string? Currency, bool? IsActive);