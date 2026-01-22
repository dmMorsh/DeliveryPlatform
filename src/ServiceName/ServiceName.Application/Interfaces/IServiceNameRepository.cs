using ServiceName.Domain.Entities;

namespace ServiceName.Application.Interfaces;

public interface IServiceNameRepository
{
    Task AddAsync(ServiceAggregate entity, CancellationToken ct = default);
    Task<ServiceAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
