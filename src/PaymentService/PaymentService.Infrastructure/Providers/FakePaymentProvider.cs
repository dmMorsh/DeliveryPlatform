using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;

namespace PaymentService.Infrastructure.Providers;

public sealed class FakePaymentProvider : IPaymentProvider
{
    private readonly HttpClient _client;
    private readonly FakePaymentOptions _options;

    public FakePaymentProvider(HttpClient client, IOptions<FakePaymentOptions> options)
    {
        _client = client;
        _options = options.Value;
        ConfigureClient();
    }

    public string Name => "FakePay";
    public IReadOnlyCollection<string> Aliases => new[] { "FakePay", "Fake", "Mock", "Stub" };

    public async Task<StartPaymentResult> StartPayment(StartPaymentRequest request, CancellationToken ct)
    {
        var payload = new FakeStartRequest(
            request.PaymentId,
            request.OrderId,
            request.AmountCents,
            request.Currency,
            request.Description,
            request.Capture,
            FormatUrl(_options.ReturnUrl, request.OrderId),
            FormatUrl(_options.FailUrl, request.OrderId));

        using var response = await _client.PostAsJsonAsync("/api/fake-payments/start", payload, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<FakeStartResponse>(cancellationToken: ct);
        if (body is null || string.IsNullOrWhiteSpace(body.ExternalPaymentId) || string.IsNullOrWhiteSpace(body.PaymentUrl))
            throw new InvalidOperationException("FakePay response is invalid");

        return new StartPaymentResult(body.ExternalPaymentId, body.PaymentUrl);
    }

    public async Task CapturePayment(string externalPaymentId, long? amountCents, string currency, CancellationToken ct)
    {
        using var response = await _client.PostAsync($"/api/fake-payments/capture/{Uri.EscapeDataString(externalPaymentId)}", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task CancelPayment(string externalPaymentId, CancellationToken ct)
    {
        using var response = await _client.PostAsync($"/api/fake-payments/cancel/{Uri.EscapeDataString(externalPaymentId)}", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RefundPayment(string externalPaymentId, long amountCents, string currency, CancellationToken ct)
    {
        using var response = await _client.PostAsync($"/api/fake-payments/refund/{Uri.EscapeDataString(externalPaymentId)}", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PaymentProviderStatus> CheckStatus(string externalPaymentId, CancellationToken ct)
    {
        var url = $"/api/fake-payments/status/{Uri.EscapeDataString(externalPaymentId)}";
        var body = await _client.GetFromJsonAsync<FakeStatusResponse>(url, ct);
        return MapStatus(body?.Status);
    }

    private void ConfigureClient()
    {
        _client.BaseAddress = new Uri(_options.BaseUrl);
        _client.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
    }

    private static PaymentProviderStatus MapStatus(string? status)
    {
        return status?.Trim().ToLowerInvariant() switch
        {
            "authorized" => PaymentProviderStatus.Authorized,
            "succeeded" => PaymentProviderStatus.Succeeded,
            "cancelled" => PaymentProviderStatus.Cancelled,
            "refunded" => PaymentProviderStatus.Refunded,
            "failed" => PaymentProviderStatus.Failed,
            _ => PaymentProviderStatus.Pending
        };
    }

    private static string FormatUrl(string template, Guid orderId)
        => template.Replace("{orderId}", orderId.ToString(), StringComparison.OrdinalIgnoreCase);

    private sealed record FakeStartRequest(
        Guid PaymentId,
        Guid OrderId,
        long AmountCents,
        string Currency,
        string Description,
        bool Capture,
        string ReturnUrl,
        string FailUrl);

    private sealed record FakeStartResponse(string ExternalPaymentId, string PaymentUrl);

    private sealed record FakeStatusResponse(string Status);
}
