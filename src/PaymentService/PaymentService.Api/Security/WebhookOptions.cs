namespace PaymentService.Api.Security;

public sealed class WebhookOptions
{
    public string SharedSecret { get; set; } = string.Empty;
    public List<string> AllowedIps { get; set; } = new();
}
