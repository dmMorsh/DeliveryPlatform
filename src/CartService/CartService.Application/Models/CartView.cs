namespace CartService.Application.Models;

public record CartView
{
    public Guid Id { get; set; }
    public IReadOnlyCollection<CartViewItem> Items { get; init; } = Array.Empty<CartViewItem>();
}

public record CartViewItem(Guid ProductId, string Name, int Price, int Quantity);