namespace CourierService.Data;

public interface IUnitOfWork
{
    /// <summary>
    /// Persist changes staged in the current DbContext (aggregates + outbox) within a transaction.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
