namespace WebApp.Models;

public class CartViewModel
{
    public Guid Id { get; set; }
    public List<CartItemViewModel> Items { get; set; } = new();
    public long TotalCents
    {
        get
        {
            var sum = 0l;
            foreach (var item in Items)
                sum += item.PriceCents * item.Quantity;
            return sum;
        }
    }

    public decimal Total => TotalCents / 100m;
}

public class CartItemViewModel
{
    public Guid ProductId { get; set; }
    public string Name { get; set; } = "";
    public int Quantity { get; set; }
    public long PriceCents { get; set; }

    public decimal Price => PriceCents / 100m;
    public decimal LineTotal => Quantity * Price;
}
