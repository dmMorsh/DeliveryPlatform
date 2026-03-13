namespace WebApp.Models;

public class ProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public long PriceCents { get; set; }
    public string? Currency { get; set; }
    public long WeightGrams { get; set; }
    public int QuantityAvailable { get; set; }
    public string? Category { get; set; }
    public string[]? Colors { get; set; }
    public string? Brand { get; set; }
    public double? Rating { get; set; }
    public bool? IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public decimal Price => PriceCents / 100m;
}
