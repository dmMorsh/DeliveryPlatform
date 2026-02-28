namespace NotificationService.Services;

public sealed class NotificationOptions
{
    public string? WebhookUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 5;
}
