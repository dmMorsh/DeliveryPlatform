using Hangfire.Server;

namespace Shared.Services;

public interface IHangfireCommandExecutor
{
    Task ExecuteAsync<TRequest>(TRequest command, PerformContext context, CancellationToken ct = default)
        where TRequest : IHangfireRetryable;
}