using Hangfire;
using Hangfire.Server;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Contracts;
using Shared.Contracts.Events;
using System.Text.Json;

namespace Shared.Services;

public class HangfireCommandExecutor<TDbContext> : IHangfireCommandExecutor
    where TDbContext : DbContext
{
    private const int MaxRetries = 5;
    private readonly IMediator _mediator;
    private readonly TDbContext _db;
    private readonly string _aggregateType;

    public HangfireCommandExecutor(IMediator mediator, TDbContext db, string aggregateType)
    {
        _mediator = mediator;
        _db = db;
        _aggregateType = string.IsNullOrWhiteSpace(aggregateType)
            ? throw new ArgumentException("Aggregate type is required.", nameof(aggregateType))
            : aggregateType;
    }

    [AutomaticRetry(Attempts = MaxRetries)]
    public async Task ExecuteAsync<TRequest>(TRequest command, PerformContext context, CancellationToken ct)
        where TRequest : IHangfireRetryable
    {
        if (await AlreadyProcessed(command, ct))
            return;

        try
        {
            await _mediator.Send(command, ct);
        }
        catch (Exception ex)
        {
            var retryCount = context?.GetJobParameter<int>("RetryCount") ?? 0;
            if (retryCount >= MaxRetries - 1)
            {
                await PublishCommandFailed(command, ex, retryCount, ct);
            }

            throw;
        }

        try
        {
            _db.Set<ProcessedCommand>().Add(new ProcessedCommand
            {
                CorrelationId = command.CorrelationId,
                CommandType = typeof(TRequest).Name,
                ProcessedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return;
        }
    }

    private async Task<bool> AlreadyProcessed<TRequest>(TRequest command, CancellationToken ct)
        where TRequest : IHangfireRetryable
    {
        return await _db.Set<ProcessedCommand>().AnyAsync(
            x => x.CorrelationId == command.CorrelationId && x.CommandType == typeof(TRequest).Name,
            ct);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation;
    }

    private async Task PublishCommandFailed<TRequest>(TRequest command, Exception ex, int retryCount, CancellationToken ct)
        where TRequest : IHangfireRetryable
    {
        var payload = JsonSerializer.Serialize(command, command.GetType());
        var evt = new CommandFailedEvent(_aggregateType)
        {
            CorrelationId = command.CorrelationId,
            CommandType = command.GetType().FullName ?? typeof(TRequest).Name,
            Payload = payload,
            Reason = ex.ToString(),
            RetryCount = retryCount + 1,
            FailedAt = DateTime.UtcNow
        };

        _db.Set<OutboxMessage>().Add(OutboxMessage.From(evt));
        await _db.SaveChangesAsync(ct);
    }
}