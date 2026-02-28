using CourierService.Domain.Aggregates;

namespace CourierService.Application.Interfaces;

public interface ICourierRepository
{
    Task<Courier?> GetCourierByIdAsync(Guid id, CancellationToken ct);
    Task<Courier?> GetCourierByPhoneAsync(string phone, CancellationToken ct);
    Task<List<Courier>> GetActiveCouriersAsync(CancellationToken ct);
    Task<Courier> CreateCourierAsync(Courier courier, CancellationToken ct);
    Task<(List<Courier> Items, int Total)> GetCouriersPagedAsync(CancellationToken ct, int page = 1, int pageSize = 20);
}
