using System.Text.Json.Serialization;

namespace PaymentService.Application.Models;

public sealed class SberbankWebhookModel
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; init; } = string.Empty;

    [JsonPropertyName("orderStatus")]
    public int OrderStatus { get; init; }
}
