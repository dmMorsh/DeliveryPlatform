namespace CatalogService.Application.Models;

public record ProductView(Guid Id, string Name, string? Description, decimal PriceCents, string Currency);