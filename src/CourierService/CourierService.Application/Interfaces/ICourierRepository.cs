using CourierService.Domain.Aggregates;

namespace CourierService.Application.Interfaces;

public interface ICourierRepository
{
    Task<Courier?> GetCourierByIdAsync(Guid id);
    Task<Courier?> GetCourierByPhoneAsync(string phone);
    Task<List<Courier>> GetActiveCouriersAsync();
    Task<Courier> CreateCourierAsync(Courier courier);
    Task<Courier?> UpdateCourierAsync(Courier updatedCourier);
    Task<(List<Courier> Items, int Total)> GetCouriersPagedAsync(int page = 1, int pageSize = 20);
}
