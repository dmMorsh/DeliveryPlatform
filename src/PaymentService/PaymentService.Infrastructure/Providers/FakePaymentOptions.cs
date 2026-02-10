namespace PaymentService.Infrastructure.Providers;

public sealed class FakePaymentOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5177";
    public string ReturnUrl { get; set; } = "http://localhost:51660/orders/{orderId}";
    public string FailUrl { get; set; } = "http://localhost:51660/orders/{orderId}";
    public int TimeoutSeconds { get; set; } = 10;
}
