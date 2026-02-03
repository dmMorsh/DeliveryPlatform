namespace WebApp.Models;

public class CheckoutViewModel
{
    public string FromAddress { get; set; } = "";
    public double FromLatitude { get; set; }
    public double FromLongitude { get; set; }

    public string ToAddress { get; set; } = "";
    public double ToLatitude { get; set; }
    public double ToLongitude { get; set; }

    public string? Description { get; set; }
    public string? CourierNote { get; set; }
    
    public long? CostCents { get; set; }
    public string? Currency { get; set; }
}
