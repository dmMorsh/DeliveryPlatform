using MediatR;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;
using Shared.Contracts;
using Shared.Contracts.Events;

namespace PaymentService.Application.Commands.ProcessYooMoneyWebhook;

public class ProcessYooMoneyWebhookCommandHandler : IRequestHandler<ProcessYooMoneyWebhookCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IPaymentProviderResolver _providers;
    private readonly IPaymentIntegrationEventMapper _eventMapper;

    public ProcessYooMoneyWebhookCommandHandler(
        IUnitOfWorkFactory factory,
        IPaymentProviderResolver providers,
        IPaymentIntegrationEventMapper eventMapper)
    {
        _factory = factory;
        _providers = providers;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(ProcessYooMoneyWebhookCommand request, CancellationToken ct)
    {
        var evt = (request.Event ?? string.Empty).Trim().ToLowerInvariant();

        if (evt.StartsWith("payment."))
            return await HandlePaymentEvent(request.Object, ct);

        if (evt.StartsWith("refund."))
            return await HandleRefundEvent(request.Object, ct);

        return ApiResponse.SuccessResponse();
    }

    private async Task<ApiResponse> HandlePaymentEvent(System.Text.Json.JsonElement model, CancellationToken ct)
    {
        var externalId = GetString(model, "id");
        if (string.IsNullOrWhiteSpace(externalId))
            return ApiResponse.ErrorResponse(ErrorCodes.Validation, "Payment id is required");

        var orderId = await _factory.ResolveOrderIdByExternalPaymentId(externalId, ct);
        if (orderId is null)
            orderId = GetGuidFromMetadata(model, "order_id");

        if (orderId is null)
            return ApiResponse.SuccessResponse();

        await using var uow = _factory.Create(orderId.Value);
        var payment = await uow.Payments.GetByOrderId(orderId.Value, ct);
        if (payment is null)
            return ApiResponse.SuccessResponse();

        var provider = _providers.Get(payment.Provider);
        var providerStatus = await provider.CheckStatus(externalId, ct);

        var prev = payment.Status;
        switch (providerStatus)
        {
            case PaymentProviderStatus.Authorized:
                payment.MarkAuthorized(externalId);
                break;
            case PaymentProviderStatus.Succeeded:
                payment.MarkCaptured(externalId);
                break;
            case PaymentProviderStatus.Cancelled:
                payment.MarkCancelled();
                break;
            case PaymentProviderStatus.Refunded:
                payment.MarkRefunded();
                break;
            case PaymentProviderStatus.Failed:
                payment.MarkFailed("Provider status failed");
                break;
        }

        var outbox = BuildOutboxIfChanged(prev, payment, "Provider status failed");
        await uow.Payments.UpsertExternalPaymentIdMap(payment.OrderId, payment.Id, externalId, payment.Provider, ct);
        await uow.SaveChangesAsync(outbox, ct);
        return ApiResponse.SuccessResponse();
    }

    private async Task<ApiResponse> HandleRefundEvent(System.Text.Json.JsonElement model, CancellationToken ct)
    {
        var paymentId = GetString(model, "payment_id");
        if (string.IsNullOrWhiteSpace(paymentId))
            return ApiResponse.SuccessResponse();

        var status = GetString(model, "status")?.Trim().ToLowerInvariant();
        if (status != "succeeded")
            return ApiResponse.SuccessResponse();

        var orderId = await _factory.ResolveOrderIdByExternalPaymentId(paymentId, ct);
        if (orderId is null)
            return ApiResponse.SuccessResponse();

        await using var uow = _factory.Create(orderId.Value);
        var payment = await uow.Payments.GetByOrderId(orderId.Value, ct);
        if (payment is null)
            return ApiResponse.SuccessResponse();

        var prev = payment.Status;
        payment.MarkRefunded();
        var outbox = BuildOutboxIfChanged(prev, payment, "Refund succeeded");
        await uow.Payments.UpsertExternalPaymentIdMap(payment.OrderId, payment.Id, paymentId, payment.Provider, ct);
        await uow.SaveChangesAsync(outbox, ct);
        return ApiResponse.SuccessResponse();
    }

    private static string? GetString(System.Text.Json.JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return null;
        return value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static Guid? GetGuidFromMetadata(System.Text.Json.JsonElement element, string key)
    {
        if (!element.TryGetProperty("metadata", out var metadata))
            return null;

        if (metadata.ValueKind != System.Text.Json.JsonValueKind.Object)
            return null;

        if (!metadata.TryGetProperty(key, out var value))
            return null;

        var raw = value.ValueKind == System.Text.Json.JsonValueKind.String ? value.GetString() : value.ToString();
        return Guid.TryParse(raw, out var guid) ? guid : null;
    }

    private List<OutboxMessage> BuildOutboxIfChanged(PaymentStatus prev, Payment payment, string reason)
    {
        if (payment.Status == prev)
            return new List<OutboxMessage>();

        IntegrationEvent? evt = payment.Status switch
        {
            PaymentStatus.Authorized => _eventMapper.MapAuthorized(payment),
            PaymentStatus.Captured => _eventMapper.MapCaptured(payment),
            PaymentStatus.Cancelled => _eventMapper.MapCancelled(payment),
            PaymentStatus.Refunded => _eventMapper.MapRefunded(payment),
            PaymentStatus.Failed => _eventMapper.MapFailed(payment, reason),
            _ => null
        };

        return evt is null ? new List<OutboxMessage>() : new List<OutboxMessage> { OutboxMessage.From(evt) };
    }
}
