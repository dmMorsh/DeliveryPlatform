namespace CatalogService.Infrastructure.ReadStore;

public sealed class ProductReadModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long PriceCents { get; set; }
    public int QuantityAvailable { get; set; }
    // additional fields for future filters
    public string? Category { get; set; }
    public string[] Colors { get; set; } = Array.Empty<string>();
    public string? Brand { get; set; }
    public double? Rating { get; set; }
    public DateTime UpdatedAt { get; set; }
}
