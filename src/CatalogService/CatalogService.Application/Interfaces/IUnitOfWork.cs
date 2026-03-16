using Shared.Contracts;

namespace CatalogService.Application.Interfaces;

public interface IUnitOfWork
{
    Task SaveChangesAsync(List<OutboxMessage> outboxMessages,
        CancellationToken ct = default);
}
