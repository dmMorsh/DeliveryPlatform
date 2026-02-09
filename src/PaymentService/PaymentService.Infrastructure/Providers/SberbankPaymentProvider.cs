using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;

namespace PaymentService.Infrastructure.Providers;

public sealed class SberbankPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _client;
    private readonly SberbankOptions _options;

    public SberbankPaymentProvider(HttpClient client, IOptions<SberbankOptions> options)
    {
        _client = client;
        _options = options.Value;
        ConfigureClient();
    }

    public string Name => "Sberbank";
    public IReadOnlyCollection<string> Aliases => new[] { "Sber", "Sberbank", "SberbankAcquiring" };

    public async Task<StartPaymentResult> StartPayment(
        StartPaymentRequest request,
        CancellationToken ct)
    {
        var payload = new Dictionary<string, string?>
        {
            ["userName"] = _options.UserName,
            ["password"] = _options.Password,
            ["orderNumber"] = request.OrderId.ToString(),
            ["amount"] = request.AmountCents.ToString(),
            ["currency"] = MapCurrencyToNumeric(request.Currency),
            ["returnUrl"] = _options.ReturnUrl,
            ["failUrl"] = _options.FailUrl,
            ["description"] = request.Description
        };

        var path = request.Capture ? _options.RegisterPath : _options.RegisterPreAuthPath;
        using var response = await _client.PostAsync(path, ToForm(payload), ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<SberbankRegisterResponse>(cancellationToken: ct);
        if (body is null || body.ErrorCode != 0 || string.IsNullOrWhiteSpace(body.OrderId) || string.IsNullOrWhiteSpace(body.FormUrl))
            throw new InvalidOperationException("Sberbank response is invalid");

        return new StartPaymentResult(body.OrderId, body.FormUrl);
    }

    public async Task<PaymentProviderStatus> CheckStatus(string externalPaymentId, CancellationToken ct)
    {
        var payload = new Dictionary<string, string?>
        {
            ["userName"] = _options.UserName,
            ["password"] = _options.Password,
            ["orderId"] = externalPaymentId
        };

        using var response = await _client.PostAsync(_options.StatusPath, ToForm(payload), ct);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<SberbankStatusResponse>(cancellationToken: ct);
        return MapStatus(body?.OrderStatus);
    }

    public async Task CapturePayment(string externalPaymentId, long? amountCents, string currency, CancellationToken ct)
    {
        var payload = new Dictionary<string, string?>
        {
            ["userName"] = _options.UserName,
            ["password"] = _options.Password,
            ["orderId"] = externalPaymentId,
            ["amount"] = amountCents?.ToString()
        };

        using var response = await _client.PostAsync(_options.DepositPath, ToForm(payload), ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelPayment(string externalPaymentId, CancellationToken ct)
    {
        var payload = new Dictionary<string, string?>
        {
            ["userName"] = _options.UserName,
            ["password"] = _options.Password,
            ["orderId"] = externalPaymentId
        };

        using var response = await _client.PostAsync(_options.ReversePath, ToForm(payload), ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RefundPayment(string externalPaymentId, long amountCents, string currency, CancellationToken ct)
    {
        var payload = new Dictionary<string, string?>
        {
            ["userName"] = _options.UserName,
            ["password"] = _options.Password,
            ["orderId"] = externalPaymentId,
            ["amount"] = amountCents.ToString()
        };

        using var response = await _client.PostAsync(_options.RefundPath, ToForm(payload), ct);
        response.EnsureSuccessStatusCode();
    }

    private void ConfigureClient()
    {
        if (_client.BaseAddress is null && !string.IsNullOrWhiteSpace(_options.BaseUrl))
            _client.BaseAddress = new Uri(_options.BaseUrl);

        if (_options.TimeoutSeconds > 0)
            _client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

    }

    private static PaymentProviderStatus MapStatus(int? orderStatus)
    {
        return orderStatus switch
        {
            1 => PaymentProviderStatus.Authorized,
            2 => PaymentProviderStatus.Succeeded,
            3 => PaymentProviderStatus.Cancelled,
            4 => PaymentProviderStatus.Refunded,
            6 => PaymentProviderStatus.Failed,
            _ => PaymentProviderStatus.Pending
        };
    }

    private static string? MapCurrencyToNumeric(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return null;

        return currency.Trim().ToUpperInvariant() switch
        {
            "RUB" => "643",
            "RUR" => "643",
            _ => null
        };
    }

    private sealed class SberbankRegisterResponse
    {
        [JsonPropertyName("errorCode")]
        public int ErrorCode { get; init; }
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; init; }
        [JsonPropertyName("orderId")]
        public string OrderId { get; init; } = string.Empty;
        [JsonPropertyName("formUrl")]
        public string FormUrl { get; init; } = string.Empty;
    }

    private sealed class SberbankStatusResponse
    {
        [JsonPropertyName("orderStatus")]
        public int OrderStatus { get; init; }
    }

    private static FormUrlEncodedContent ToForm(Dictionary<string, string?> payload)
        => new(payload.Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => new KeyValuePair<string, string>(kv.Key, kv.Value!)));
}
