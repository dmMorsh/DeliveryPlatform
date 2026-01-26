namespace GatewayApi.Contracts.Catalog;

public record UpdateCatalogProductRequest
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public decimal PriceCents { get; init; }
    public string? Currency { get; init; }
    public bool? IsActive { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
}