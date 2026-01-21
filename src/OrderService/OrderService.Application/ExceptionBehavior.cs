using MediatR;
using Microsoft.Extensions.Logging;

namespace OrderService.Application;

public class ExceptionBehavior<TReq, TRes> : IPipelineBehavior<TReq, TRes> 
    where TReq : IRequest<TRes>
{
    private readonly ILogger<ExceptionBehavior<TReq, TRes>> _logger;

    public ExceptionBehavior(ILogger<ExceptionBehavior<TReq, TRes>> logger)
    {
        _logger = logger;
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            throw;
        }
    }
}
