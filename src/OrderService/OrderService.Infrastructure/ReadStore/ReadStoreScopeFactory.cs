using Microsoft.Extensions.DependencyInjection;

namespace OrderService.Infrastructure.ReadStore;

public sealed class ReadStoreScopeFactory
{
    public ReadStoreScopeFactory(IServiceScopeFactory scopeFactory)
    {
        ScopeFactory = scopeFactory;
    }

    public IServiceScopeFactory ScopeFactory { get; }
}
