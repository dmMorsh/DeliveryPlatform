using PaymentService.Domain.Aggregates;

namespace PaymentService.Application.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment entity, CancellationToken ct = default);
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
