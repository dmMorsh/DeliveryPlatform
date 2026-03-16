namespace CatalogService.Api.Contracts;

public record UpdateProductRequest(string? Name, string? Description, long? PriceCents, string? Currency, bool? IsActive);