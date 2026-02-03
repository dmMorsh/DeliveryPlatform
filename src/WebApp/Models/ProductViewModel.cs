namespace WebApp.Models;

public class ProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public long PriceCents { get; set; }
    public string? Currency { get; set; }
    public long WeightGrams { get; set; }

    public decimal Price => PriceCents / 100m;
}