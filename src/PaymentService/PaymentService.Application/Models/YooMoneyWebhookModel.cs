using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaymentService.Application.Models;

public sealed class YooMoneyWebhookModel
{
    [JsonPropertyName("event")]
    public string Event { get; init; } = string.Empty;

    [JsonPropertyName("object")]
    public JsonElement Object { get; init; }
}
