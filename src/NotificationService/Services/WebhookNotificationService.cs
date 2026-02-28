using Microsoft.Extensions.Options;

namespace NotificationService.Services;

public sealed class WebhookNotificationService : INotificationService
{
    private readonly HttpClient _httpClient;
    private readonly NotificationOptions _options;
    private readonly ILogger<WebhookNotificationService> _logger;

    public WebhookNotificationService(
        HttpClient httpClient,
        IOptions<NotificationOptions> options,
        ILogger<WebhookNotificationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
    }

    public async Task SendNotificationAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookUrl))
        {
            _logger.LogWarning("Notification webhook URL is not configured. Dropping message.");
            return;
        }

        try
        {
            var payload = new
            {
                message,
                timestamp = DateTime.UtcNow.ToString("O")
            };

            var response = await _httpClient.PostAsJsonAsync(_options.WebhookUrl, payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Notification webhook failed: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification webhook failed");
        }
    }
}
