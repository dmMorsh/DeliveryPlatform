using Microsoft.Extensions.Options;

namespace PaymentService.Api.Security;

public sealed class WebhookValidator : IWebhookValidator
{
    private readonly WebhookOptions _options;

    public WebhookValidator(IOptions<WebhookOptions> options)
    {
        _options = options.Value;
    }

    public bool IsValid(HttpContext context)
    {
        if (_options.AllowedIps.Count > 0)
        {
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrWhiteSpace(remoteIp) || !_options.AllowedIps.Contains(remoteIp))
                return false;
        }

        if (!string.IsNullOrWhiteSpace(_options.SharedSecret))
        {
            if (!context.Request.Headers.TryGetValue("X-Webhook-Secret", out var header))
                return false;
            if (!string.Equals(header.ToString(), _options.SharedSecret, StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
