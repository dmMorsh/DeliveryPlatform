using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Shared.Services;

public class HangfireRetryBehavior<TReq, TRes> : IPipelineBehavior<TReq, TRes>
    where TReq : IRequest<TRes>
{
    private readonly int _delaySec = 30;
    private readonly IBackgroundJobClient _jobs;

    public HangfireRetryBehavior(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    public async Task<TRes> Handle(TReq request, RequestHandlerDelegate<TRes> next, CancellationToken ct)
    {
        try
        {
            return await next();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (request is IHangfireRetryable retryable && retryable.CorrelationId != Guid.Empty)
            {
                _jobs.Schedule<IHangfireCommandExecutor>(x =>
                    x.ExecuteAsync(retryable, null, CancellationToken.None), TimeSpan.FromSeconds(_delaySec));
            }

            throw;
        }
    }
}