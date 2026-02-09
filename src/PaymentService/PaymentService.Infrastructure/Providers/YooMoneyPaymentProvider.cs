using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;

namespace PaymentService.Infrastructure.Providers;

public sealed class YooMoneyPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _client;
    private readonly YooMoneyOptions _options;

    public YooMoneyPaymentProvider(HttpClient client, IOptions<YooMoneyOptions> options)
    {
        _client = client;
        _options = options.Value;
        ConfigureClient();
    }

    public string Name => "YooMoney";
    public IReadOnlyCollection<string> Aliases => new[] { "YooMoney", "YooKassa", "Yoo" };

    public async Task<StartPaymentResult> StartPayment(
        StartPaymentRequest request,
        CancellationToken ct)
    {
        var payload = new CreatePaymentRequest
        {
            Amount = new AmountModel
            {
                Value = FormatAmount(request.AmountCents),
                Currency = request.Currency
            },
            Capture = request.Capture,
            Confirmation = new ConfirmationModel
            {
                Type = "redirect",
                ReturnUrl = _options.ReturnUrl
            },
            Description = request.Description,
            Metadata = new Dictionary<string, string>
            {
                ["payment_id"] = request.PaymentId.ToString(),
                ["order_id"] = request.OrderId.ToString()
            }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, _options.StartPath)
        {
            Content = JsonContent.Create(payload)
        };
        message.Headers.Add("Idempotence-Key", Guid.NewGuid().ToString());

        using var response = await _client.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<CreatePaymentResponse>(cancellationToken: ct);
        var confirmationUrl = body?.Confirmation?.ConfirmationUrl;
        if (body is null || string.IsNullOrWhiteSpace(body.Id) || string.IsNullOrWhiteSpace(confirmationUrl))
            throw new InvalidOperationException("YooMoney response is invalid");

        return new StartPaymentResult(body.Id, confirmationUrl);
    }

    public async Task<PaymentProviderStatus> CheckStatus(string externalPaymentId, CancellationToken ct)
    {
        var url = $"{_options.StatusPath}/{Uri.EscapeDataString(externalPaymentId)}";
        var body = await _client.GetFromJsonAsync<PaymentStatusResponse>(url, ct);
        return MapStatus(body?.Status);
    }

    public async Task CapturePayment(string externalPaymentId, long? amountCents, string currency, CancellationToken ct)
    {
        var payload = amountCents is null
            ? null
            : new CapturePaymentRequest
            {
                Amount = new AmountModel
                {
                    Value = FormatAmount(amountCents.Value),
                    Currency = currency
                }
            };

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.StatusPath}/{Uri.EscapeDataString(externalPaymentId)}/capture")
        {
            Content = payload is null ? null : JsonContent.Create(payload)
        };
        message.Headers.Add("Idempotence-Key", Guid.NewGuid().ToString());

        using var response = await _client.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelPayment(string externalPaymentId, CancellationToken ct)
    {
        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_options.StatusPath}/{Uri.EscapeDataString(externalPaymentId)}/cancel");
        message.Headers.Add("Idempotence-Key", Guid.NewGuid().ToString());

        using var response = await _client.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RefundPayment(string externalPaymentId, long amountCents, string currency, CancellationToken ct)
    {
        var payload = new RefundPaymentRequest
        {
            PaymentId = externalPaymentId,
                Amount = new AmountModel
            {
                Value = FormatAmount(amountCents),
                Currency = currency
            }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, _options.RefundPath)
        {
            Content = JsonContent.Create(payload)
        };
        message.Headers.Add("Idempotence-Key", Guid.NewGuid().ToString());

        using var response = await _client.SendAsync(message, ct);
        response.EnsureSuccessStatusCode();
    }

    private void ConfigureClient()
    {
        if (_client.BaseAddress is null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
            _client.BaseAddress = new Uri(_options.BaseUrl);

        if (_options.TimeoutSeconds > 0)
            _client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        if (!string.IsNullOrWhiteSpace(_options.ShopId) && !string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            var raw = $"{_options.ShopId}:{_options.SecretKey}";
            var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
            _client.DefaultRequestHeaders.Remove("Authorization");
            _client.DefaultRequestHeaders.Add("Authorization", $"Basic {encoded}");
        }
    }

    private static PaymentProviderStatus MapStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "waiting_for_capture" => PaymentProviderStatus.Authorized,
            "succeeded" => PaymentProviderStatus.Succeeded,
            "canceled" => PaymentProviderStatus.Cancelled,
            _ => PaymentProviderStatus.Pending
        };
    }

    private static string FormatAmount(long amountCents)
        => (amountCents / 100m).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

    private sealed class CreatePaymentRequest
    {
        [JsonPropertyName("amount")]
        public AmountModel Amount { get; init; } = new();
        [JsonPropertyName("capture")]
        public bool Capture { get; init; }
        [JsonPropertyName("confirmation")]
        public ConfirmationModel Confirmation { get; init; } = new();
        [JsonPropertyName("description")]
        public string Description { get; init; } = string.Empty;
        [JsonPropertyName("metadata")]
        public Dictionary<string, string> Metadata { get; init; } = new();
    }

    private sealed class AmountModel
    {
        [JsonPropertyName("value")]
        public string Value { get; init; } = string.Empty;
        [JsonPropertyName("currency")]
        public string Currency { get; init; } = string.Empty;
    }

    private sealed class ConfirmationModel
    {
        [JsonPropertyName("type")]
        public string Type { get; init; } = string.Empty;
        [JsonPropertyName("return_url")]
        public string ReturnUrl { get; init; } = string.Empty;
    }

    private sealed class CreatePaymentResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; init; } = string.Empty;
        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;
        [JsonPropertyName("confirmation")]
        public ConfirmationResponse? Confirmation { get; init; }
    }

    private sealed class CapturePaymentRequest
    {
        [JsonPropertyName("amount")]
        public AmountModel Amount { get; init; } = new();
    }

    private sealed class RefundPaymentRequest
    {
        [JsonPropertyName("payment_id")]
        public string PaymentId { get; init; } = string.Empty;
        [JsonPropertyName("amount")]
        public AmountModel Amount { get; init; } = new();
    }

    private sealed class ConfirmationResponse
    {
        [JsonPropertyName("confirmation_url")]
        public string ConfirmationUrl { get; init; } = string.Empty;
    }

    private sealed class PaymentStatusResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;
    }
}
