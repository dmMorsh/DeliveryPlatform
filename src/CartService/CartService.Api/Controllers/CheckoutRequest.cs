namespace CartService.Api.Controllers;

public class CheckoutRequest
{
    public string DeliveryWindowFrom { get; set; } = null!;
    public string DeliveryWindowTo { get; set; } = null!;
}
