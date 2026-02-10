using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Mapping;
using DeliveryService.Application.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeliveryService.Infrastructure.Services;

public class AssignmentScheduler : BackgroundService
{
    private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AssignmentScheduler> _logger;

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
        var repository = scope.ServiceProvider.GetRequiredService<IDeliveryRepository>();
        var assignment = scope.ServiceProvider.GetRequiredService<IAssignmentService>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var mapper = scope.ServiceProvider.GetRequiredService<IDeliveryEventMapper>();

        var now = DateTime.UtcNow;
        var deliveries = await repository.GetAssigningDeliveriesAsync(now, ct);
        if (deliveries.Count == 0)
            return;

        foreach (var delivery in deliveries)
        {
            var offered = await assignment.OfferNextCourierAsync(delivery, ct);
            if (!offered)
                continue;

            await repository.UpdateAsync(delivery, ct);

            var outbox = delivery.DomainEvents
                .Select(mapper.MapFromDomainEvent)
                .Where(e => e != null)
                .Select(OutboxMessage.From!)
                .ToList();

            await uow.SaveChangesAsync(outbox, ct);
            delivery.ClearDomainEvents();
        }
    }
}
