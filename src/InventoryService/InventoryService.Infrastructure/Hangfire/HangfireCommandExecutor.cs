using Hangfire;
using Hangfire.Server;
using InventoryService.Application.Commands.ReserveStock;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Models;
using InventoryService.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;

namespace InventoryService.Infrastructure.Hangfire;

public class HangfireCommandExecutor : IHangfireCommandExecutor
{
    private const int MaxRetries = 5;
    private readonly IMediator _mediator;
    private readonly InventoryDbContext _db;
    private readonly IStockIntegrationEventMapper _eventMapper;
    private readonly IUnitOfWorkFactory _factory;

    public HangfireCommandExecutor(IMediator mediator, InventoryDbContext db, IStockIntegrationEventMapper eventMapper, IUnitOfWorkFactory factory)
    {
        _mediator = mediator;
        _db = db;
        _eventMapper = eventMapper;
        _factory = factory;
    }
    
    [AutomaticRetry(Attempts = MaxRetries)]
    public async Task ExecuteAsync<TRequest>(
        TRequest command,
        PerformContext context,
        CancellationToken ct)
        where TRequest : IHangfireRetryable
    {
        if (await AlreadyProcessed(command, ct))
            return;
        try
        {
            await _mediator.Send(command, ct);
        }
        catch (Exception e)
        {
            var retryCount = context.GetJobParameter<int>("RetryCount");
            var maxRetries = MaxRetries;

            if (retryCount >= maxRetries - 1)
            {
                await PublishInventoryReserveFailed(command, e);
            }
            throw;
        }

        _db.ProcessedCommands.Add(new ProcessedCommand
        {
            CorrelationId = command.CorrelationId,
            CommandType = typeof(TRequest).Name,
            ProcessedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }

    private async Task<bool> AlreadyProcessed<TRequest>(TRequest command, CancellationToken ct)
        where TRequest : IHangfireRetryable
    {
        return await _db.ProcessedCommands.AnyAsync(
            x => x.CorrelationId == command.CorrelationId &&
                 x.CommandType == typeof(TRequest).Name,
            ct);
    }

    private async Task PublishInventoryReserveFailed<TRequest>(TRequest command, Exception e) where TRequest : IHangfireRetryable
    {
        if (command is ReserveStockCommand reserveStockCommand)
        {
            var failedItems = reserveStockCommand.ReserveStockModels
                .Select(m=> new FailedStockItemSnapshot
                {
                    ProductId = m.ProductId, Quantity = m.Quantity, Reason = e.Message
                }).ToList();
            var reserveFailedEvent = _eventMapper.MapStockReserveFailedEvent(reserveStockCommand.OrderId, failedItems);
            
            var outboxMessages = new List<OutboxMessage> { OutboxMessage.From(reserveFailedEvent) };
            await using var uow = _factory.Create(failedItems.First().ProductId);
            await uow.SaveChangesAsync(outboxMessages, CancellationToken.None);
        }
    }
}
