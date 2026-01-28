using OrderService.Domain.Aggregates;
using OrderService.Domain.Entities;

namespace OrderService.Application.Mapping;

public static class OrderFactory
{
    public static Order CreateNew(
        Guid clientId,
        string fromAddress,
        string toAddress,
        double fromLatitude,
        double fromLongitude,
        double toLatitude,
        double toLongitude,
        string? description,
        int weightGrams,
        long costCents,
        string? currency,
        string? courierNote,
        List<OrderItem>? items = null)
    {
        return Order.Create(clientId, fromAddress, toAddress, fromLatitude, fromLongitude, toLatitude, toLongitude, description, weightGrams, costCents, currency, courierNote, items);
    }
}
