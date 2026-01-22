using ServiceName.Application.Interfaces;
using ServiceName.Domain.Entities;

namespace ServiceName.Infrastructure.Repositories;

public class ServiceNameRepository : IServiceNameRepository, IUnitOfWork
{
    private static readonly List<ServiceAggregate> _store = new();

    public Task AddAsync(ServiceAggregate entity, CancellationToken ct = default)
    {
        _store.Add(entity);
        return Task.CompletedTask;
    }

    public Task<ServiceAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = _store.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(item);
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        // In-memory store - nothing to persist
        return Task.CompletedTask;
    }
}
