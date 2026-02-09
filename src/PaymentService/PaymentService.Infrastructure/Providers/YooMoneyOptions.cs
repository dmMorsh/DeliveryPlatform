namespace PaymentService.Infrastructure.Providers;

public sealed class YooMoneyOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string StartPath { get; set; } = "/v3/payments";
    public string StatusPath { get; set; } = "/v3/payments";
    public string RefundPath { get; set; } = "/v3/refunds";
    public string SecretKey { get; set; } = string.Empty;
    public string ShopId { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string FailUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}
