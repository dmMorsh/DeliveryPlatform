using Hangfire;
using InventoryService.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Application.MediatR;

public class HangfireRetryBehavior<TReq, TRes>
    : IPipelineBehavior<TReq, TRes> where TReq : IRequest<TRes>
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireRetryBehavior(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    public async Task<TRes> Handle(
        TReq request,
        RequestHandlerDelegate<TRes> next,
        CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (DbUpdateConcurrencyException)
        {
            // важно: только idempotent команды
            if (request is IHangfireRetryable retryable)
            {
                // Do not capture the request cancellation token for a future job execution.
                _jobs.Schedule<IHangfireCommandExecutor>(x =>
                    x.ExecuteAsync(retryable, null, CancellationToken.None), TimeSpan.FromSeconds(30));
            }

            throw;
        }
    }
}
