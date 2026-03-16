using MediatR;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;
using Shared.Contracts;
using Shared.Contracts.Events;
using Shared.Utilities;

namespace PaymentService.Application.Commands.ProcessSberbankWebhook;

public class ProcessSberbankWebhookCommandHandler : IRequestHandler<ProcessSberbankWebhookCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IPaymentProviderResolver _providers;
    private readonly IPaymentIntegrationEventMapper _eventMapper;

    public ProcessSberbankWebhookCommandHandler(
        IUnitOfWorkFactory factory,
        IPaymentProviderResolver providers,
        IPaymentIntegrationEventMapper eventMapper)
    {
        _factory = factory;
        _providers = providers;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(ProcessSberbankWebhookCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId))
            return ApiResponse.ErrorResponse(ErrorCodes.Validation, "OrderId is required");

        var orderId = await _factory.ResolveOrderIdByExternalPaymentId(request.OrderId, ct);
        if (orderId is null)
            return ApiResponse.SuccessResponse();

        await using var uow = _factory.Create(orderId.Value);
        var payment = await uow.Payments.GetByOrderId(orderId.Value, ct);
        if (payment is null)
            return ApiResponse.SuccessResponse();

        var provider = _providers.Get(payment.Provider);
        var providerStatus = await provider.CheckStatus(request.OrderId, ct);

        var prev = payment.Status;
        switch (providerStatus)
        {
            case PaymentProviderStatus.Authorized:
                payment.MarkAuthorized(request.OrderId);
                break;
            case PaymentProviderStatus.Succeeded:
                payment.MarkCaptured(request.OrderId);
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
        await uow.Payments.UpsertExternalPaymentIdMap(payment.OrderId, payment.Id, request.OrderId, payment.Provider, ct);
        await uow.SaveChangesAsync(outbox, ct);
        return ApiResponse.SuccessResponse();
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
