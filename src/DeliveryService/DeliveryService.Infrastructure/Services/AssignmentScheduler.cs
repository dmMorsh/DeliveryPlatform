using System.Diagnostics.Metrics;
using DeliveryService.Application.Interfaces;
using DeliveryService.Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace DeliveryService.Infrastructure.Services;

public class AssignmentScheduler : BackgroundService
{
    private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReconcileInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NoCourierRetryDelay = TimeSpan.FromMinutes(1);
    private const int BatchSize = 25;

    private static readonly Meter Meter = new("DeliveryService.Assignment", "1.0.0");
    private static readonly Counter<long> DequeueTotal = Meter.CreateCounter<long>("delivery_assignment_dequeue_total");
    private static readonly Counter<long> DequeueEmptyTotal = Meter.CreateCounter<long>("delivery_assignment_dequeue_empty_total");
    private static readonly Counter<long> OfferTotal = Meter.CreateCounter<long>("delivery_assignment_offer_total");
    private static readonly Counter<long> OfferSuccessTotal = Meter.CreateCounter<long>("delivery_assignment_offer_success_total");
    private static readonly Counter<long> OfferRetryTotal = Meter.CreateCounter<long>("delivery_assignment_offer_retry_total");
    private static readonly Counter<long> ReconcileRunsTotal = Meter.CreateCounter<long>("delivery_assignment_reconcile_total");
    private static readonly Counter<long> ReconcileAddedTotal = Meter.CreateCounter<long>("delivery_assignment_reconcile_added_total");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AssignmentScheduler> _logger;
    private DateTime _nextReconcile = DateTime.UtcNow;

    public AssignmentScheduler(IServiceScopeFactory scopeFactory, ILogger<AssignmentScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Process(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AssignmentScheduler error");
            }

            await Task.Delay(PollDelay, stoppingToken);
        }
    }

    private async Task Process(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var deliveryRepository = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
        var assignment = scope.ServiceProvider.GetRequiredService<IAssignmentService>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var mapper = scope.ServiceProvider.GetRequiredService<IDeliveryEventMapper>();
        var queue = scope.ServiceProvider.GetRequiredService<IAssignmentQueue>();

        var now = DateTime.UtcNow;
        if (now >= _nextReconcile)
        {
            var added = await ReconcileQueue(deliveryRepository, queue, now, ct);
            ReconcileRunsTotal.Add(1);
            if (added > 0)
                ReconcileAddedTotal.Add(added);
            _nextReconcile = now.Add(ReconcileInterval);
        }

        for (var i = 0; i < BatchSize; i++)
        {
            var deliveryId = await queue.DequeueReadyAsync(now, ct);
            if (!deliveryId.HasValue)
            {
                DequeueEmptyTotal.Add(1);
                break;
            }

            DequeueTotal.Add(1);
            var delivery = await deliveryRepository.GetByIdAsync(deliveryId.Value, ct);
            if (delivery == null)
                continue;

            if (delivery.Status != DeliveryStatus.Assigning)
                continue;

            if (delivery.CurrentOfferExpiresAt.HasValue && delivery.CurrentOfferExpiresAt > now)
            {
                await queue.EnqueueAsync(delivery.Id, delivery.CurrentOfferExpiresAt.Value, false, ct);
                continue;
            }

            OfferTotal.Add(1);
            var offered = await assignment.OfferNextCourierAsync(delivery, ct);
            if (!offered)
            {
                OfferRetryTotal.Add(1);
                await queue.EnqueueAsync(delivery.Id, now.Add(NoCourierRetryDelay), false, ct);
                continue;
            }

            OfferSuccessTotal.Add(1);
            var outbox = delivery.DomainEvents
                .Select(mapper.MapFromDomainEvent)
                .Where(e => e != null)
                .Select(OutboxMessage.From!)
                .ToList();

            await uow.SaveChangesAsync(outbox, ct);
            delivery.ClearDomainEvents();
        }
    }

    private static async Task<long> ReconcileQueue(
        IDeliveryRepository repository,
        IAssignmentQueue queue,
        DateTime now,
        CancellationToken ct)
    {
        var deliveries = await repository.GetAssigningDeliveriesAsync(now, ct);
        if (deliveries.Count == 0)
            return 0;

        long added = 0;
        foreach (var delivery in deliveries)
        {
            var nextAt = delivery.CurrentOfferExpiresAt ?? now;
            await queue.EnqueueAsync(delivery.Id, nextAt, true, ct);
            added++;
        }

        return added;
    }
}
