using DeliveryService.Application.Commands.CreateDeliveryFromOrder;
using DeliveryService.Domain.Aggregates;

namespace DeliveryService.Application.Mapping;

public static class DeliveryFactory
{
    public static Delivery CreateFromOrder(CreateDeliveryFromOrderCommand request)
    {
        return Delivery.Create(
            request.OrderId,
            request.ClientId,
            request.FromAddress,
            request.ToAddress,
            request.FromLatitude,
            request.FromLongitude,
            request.ToLatitude,
            request.ToLongitude);
    }
}
