using CourierService.Application.Models;

namespace CourierService.Application.Interfaces;

public interface ICourierActiveCourierListCache
{
    Task<List<CourierView>?> GetAsync(CancellationToken ct);
    Task SetAsync(List<CourierView> views, CancellationToken ct);
    Task RemoveAsync(CancellationToken ct);
}
