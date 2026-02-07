using PaymentService.Application.Interfaces;
using PaymentService.Domain.Aggregates;

namespace PaymentService.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository, IUnitOfWork
{
    private static readonly List<Payment> _store = new();

    public Task AddAsync(Payment entity, CancellationToken ct = default)
    {
        _store.Add(entity);
        return Task.CompletedTask;
    }

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default)
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
