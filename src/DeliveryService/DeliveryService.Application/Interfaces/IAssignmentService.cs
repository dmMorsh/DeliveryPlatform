using DeliveryService.Domain.Aggregates;

namespace DeliveryService.Application.Interfaces;

public interface IAssignmentService
{
    Task<bool> OfferNextCourierAsync(Delivery delivery, CancellationToken ct = default);
}
