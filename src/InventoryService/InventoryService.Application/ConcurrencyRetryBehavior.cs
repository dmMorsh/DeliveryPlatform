using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryService.Application;

public class ConcurrencyRetryBehavior<TReq, TRes>
    : IPipelineBehavior<TReq, TRes> where TReq : IRequest<TRes>
{
    private readonly ILogger<ConcurrencyRetryBehavior<TReq, TRes>> _logger;

    public ConcurrencyRetryBehavior(ILogger<ConcurrencyRetryBehavior<TReq, TRes>> logger)
    {
        _logger = logger;
    }

    public async Task<TRes> Handle(
        TReq request,
        RequestHandlerDelegate<TRes> next,
        CancellationToken ct)
    {
        const int maxRetries = 3;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await next();
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < maxRetries)
            {
                _logger.LogWarning(
                    ex,
                    "Concurrency conflict on {Request}. Retry {Attempt}/{Max}",
                    typeof(TReq).Name,
                    attempt,
                    maxRetries
                );
            }
        }

        // последняя попытка
        return await next();
    }
}
