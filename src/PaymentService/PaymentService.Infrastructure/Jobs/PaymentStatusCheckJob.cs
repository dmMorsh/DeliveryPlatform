using Hangfire;
using Microsoft.Extensions.Options;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;
using Shared.Contracts;
using Shared.Contracts.Events;

namespace PaymentService.Infrastructure.Jobs;

public class PaymentStatusCheckJob
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IPaymentProviderResolver _providers;
    private readonly IBackgroundJobClient _jobs;
    private readonly PaymentStatusCheckOptions _options;
    private readonly IPaymentIntegrationEventMapper _eventMapper;

    public PaymentStatusCheckJob(
        IUnitOfWorkFactory factory,
        IPaymentProviderResolver providers,
        IBackgroundJobClient jobs,
        IOptions<PaymentStatusCheckOptions> options,
        IPaymentIntegrationEventMapper eventMapper)
    {
        _factory = factory;
        _providers = providers;
        _jobs = jobs;
        _options = options.Value;
        _eventMapper = eventMapper;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task Check(Guid orderId, int attempt, CancellationToken ct = default)
    {
        await using var uow = _factory.Create(orderId);
        var payment = await uow.Payments.GetByOrderId(orderId, ct);
        if (payment is null)
            return;

        if (payment.Status is PaymentStatus.Captured or PaymentStatus.Cancelled or PaymentStatus.Refunded or PaymentStatus.Failed)
            return;

        if (string.IsNullOrWhiteSpace(payment.ExternalPaymentId))
            return;

        var provider = _providers.Get(payment.Provider);
        var status = await provider.CheckStatus(payment.ExternalPaymentId, ct);

        var prev = payment.Status;
        switch (status)
        {
            case PaymentProviderStatus.Authorized:
                payment.MarkAuthorized(payment.ExternalPaymentId);
                break;
            case PaymentProviderStatus.Succeeded:
                payment.MarkCaptured(payment.ExternalPaymentId);
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
        await uow.SaveChangesAsync(outbox, ct);
        if (!string.IsNullOrWhiteSpace(payment.ExternalPaymentId))
            await uow.Payments.UpsertExternalPaymentIdMap(payment.OrderId, payment.Id, payment.ExternalPaymentId, payment.Provider, ct);

        if (status is PaymentProviderStatus.Pending or PaymentProviderStatus.Authorized)
            ScheduleNext(orderId, attempt + 1);
    }

    private void ScheduleNext(Guid orderId, int attempt)
    {
        if (_options.DelaysSeconds.Length == 0)
            return;

        if (attempt >= _options.DelaysSeconds.Length)
            return;

        var index = Math.Min(attempt, _options.DelaysSeconds.Length - 1);
        var delaySeconds = _options.DelaysSeconds[index];
        _jobs.Schedule<PaymentStatusCheckJob>(
            job => job.Check(orderId, attempt),
            TimeSpan.FromSeconds(delaySeconds));
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
