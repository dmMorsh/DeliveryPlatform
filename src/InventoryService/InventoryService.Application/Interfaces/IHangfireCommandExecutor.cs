using Hangfire.Server;

namespace InventoryService.Application.Interfaces;

public interface IHangfireCommandExecutor
{
    Task ExecuteAsync<TRequest>(
        TRequest command, PerformContext context,
        CancellationToken ct = default)
        where TRequest : IHangfireRetryable;
}