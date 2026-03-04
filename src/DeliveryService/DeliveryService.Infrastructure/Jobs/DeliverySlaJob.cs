using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Models;
using DeliveryService.Domain.Aggregates;
using DeliveryService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace DeliveryService.Infrastructure.Jobs;

[DisableConcurrentExecution(60 * 60)]
public sealed class DeliverySlaJob : IDeliverySlaJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDeliveryEventMapper _eventMapper;
    private readonly IAssignmentQueue _assignmentQueue;
    private readonly ILogger<DeliverySlaJob> _logger;
    private readonly int _batchSize;
    private readonly int _maxBatches;
    private readonly TimeSpan _pickupTtl;
    private readonly TimeSpan _transitTtl;
    private readonly int _reassignMaxAttempts;
    private readonly TimeSpan _reassignCooldown;

    public DeliverySlaJob(
        IDeliveryEventMapper eventMapper,
        IAssignmentQueue assignmentQueue,
        IConfiguration config,
        ILogger<DeliverySlaJob> logger, IServiceScopeFactory scopeFactory)
    {
        _eventMapper = eventMapper;
        _assignmentQueue = assignmentQueue;
        _logger = logger;
        _scopeFactory = scopeFactory;

        var pickupMinutes = int.TryParse(config["Delivery:SlaPickupMinutes"], out var pickupValue) ? pickupValue : 60;
        var transitMinutes = int.TryParse(config["Delivery:SlaTransitMinutes"], out var transitValue) ? transitValue : 180;
        _batchSize = int.TryParse(config["Delivery:SlaBatchSize"], out var batchValue) ? batchValue : 200;
        _maxBatches = int.TryParse(config["Delivery:SlaMaxBatches"], out var maxValue) ? maxValue : 20;
        _reassignMaxAttempts = int.TryParse(config["Delivery:ReassignMaxAttempts"], out var maxAttempts) ? maxAttempts : 3;
        var cooldownMinutes = int.TryParse(config["Delivery:ReassignCooldownMinutes"], out var cooldownValue) ? cooldownValue : 10;
        _pickupTtl = pickupMinutes <= 0 ? TimeSpan.Zero : TimeSpan.FromMinutes(pickupMinutes);
        _transitTtl = transitMinutes <= 0 ? TimeSpan.Zero : TimeSpan.FromMinutes(transitMinutes);
        _reassignCooldown = cooldownMinutes <= 0 ? TimeSpan.Zero : TimeSpan.FromMinutes(cooldownMinutes);
    }

    public async Task ExecuteAsync(CancellationToken ct)
    {
        if (_pickupTtl == TimeSpan.Zero && _transitTtl == TimeSpan.Zero)
        {
            _logger.LogInformation("Delivery SLA is disabled.");
            return;
        }

        var now = DateTime.UtcNow;

        var batches = 0;
        
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DeliveryDbContext>();
        
        while (!ct.IsCancellationRequested && batches < _maxBatches)
        {
            var pickupCandidates = await db.Deliveries
                .Where(d => d.Status == DeliveryStatus.Assigned
                            && d.PickupTimeoutNotifiedAt == null
                            && d.AssignedAt != null)
                .OrderBy(d => d.AssignedAt)
                .Take(_batchSize)
                .ToListAsync(ct);

            var transitCandidates = await db.Deliveries
                .Where(d => d.Status == DeliveryStatus.InDelivery
                            && d.TransitTimeoutNotifiedAt == null
                            && d.InTransitAt != null)
                .OrderBy(d => d.InTransitAt)
                .Take(_batchSize)
                .ToListAsync(ct);

            if (pickupCandidates.Count == 0 && transitCandidates.Count == 0)
                break;

            var outbox = new List<OutboxMessage>();
            var toEnqueue = new List<Guid>();

            foreach (var delivery in pickupCandidates)
            {
                var ttlMinutes = delivery.DeliveryPickupSlaMinutes ?? (int)_pickupTtl.TotalMinutes;
                if (ttlMinutes <= 0 || !delivery.AssignedAt.HasValue)
                    continue;
                var cutoff = delivery.AssignedAt.Value.AddMinutes(ttlMinutes);
                if (cutoff > now)
                    continue;

                var previousCourierId = delivery.CourierId;
                var assignedAt = delivery.AssignedAt;
                delivery.SetPickupTimeoutNotifiedAt(now);
                if (delivery.CanReassign(_reassignMaxAttempts, _reassignCooldown, now))
                {
                    delivery.ResetAssignment("pickup_timeout");
                    toEnqueue.Add(delivery.Id);
                }
                outbox.Add(OutboxMessage.From(new DeliveryPickupTimeoutEvent
                {
                    DeliveryId = delivery.Id,
                    OrderId = delivery.OrderId,
                    CourierId = previousCourierId,
                    AssignedAt = assignedAt,
                    DetectedAt = now
                }));

                if (delivery.DomainEvents.Count > 0)
                {
                    var domainOutbox = delivery.DomainEvents
                        .Select(_eventMapper.MapFromDomainEvent)
                        .Where(e => e != null)
                        .Select(OutboxMessage.From!)
                        .ToList();
                    outbox.AddRange(domainOutbox);
                    delivery.ClearDomainEvents();
                }
            }

            foreach (var delivery in transitCandidates)
            {
                var ttlMinutes = delivery.DeliveryTransitSlaMinutes ?? (int)_transitTtl.TotalMinutes;
                if (ttlMinutes <= 0 || !delivery.InTransitAt.HasValue)
                    continue;
                var cutoff = delivery.InTransitAt.Value.AddMinutes(ttlMinutes);
                if (cutoff > now)
                    continue;

                delivery.SetTransitTimeoutNotifiedAt(now);
                outbox.Add(OutboxMessage.From(new DeliveryInTransitTimeoutEvent
                {
                    DeliveryId = delivery.Id,
                    OrderId = delivery.OrderId,
                    CourierId = delivery.CourierId,
                    InTransitAt = delivery.InTransitAt,
                    DetectedAt = now
                }));
            }

            if (outbox.Count > 0)
            {
                db.OutboxMessages.AddRange(outbox);
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Delivery SLA job emitted {Count} events", outbox.Count);
                foreach (var deliveryId in toEnqueue)
                    await _assignmentQueue.EnqueueAsync(deliveryId, now, false, ct);
            }

            batches += 1;
            if (pickupCandidates.Count < _batchSize && transitCandidates.Count < _batchSize)
                break;
        }
    }
}

public interface IDeliverySlaJob
{
    Task ExecuteAsync(CancellationToken ct);
}
