using OrderService.Application.Commands.CreateOrder;
using OrderService.Domain.Aggregates;
using OrderService.Domain.Entities;

namespace OrderService.Application.Mapping;

public static class OrderFactory
{ 
    public static Order CreateNew(CreateOrderCommand command)
    {
        return Order.Create(
            clientId: command.ClientId,
            fromAddress: command.FromAddress,
            toAddress: command.ToAddress,
            fromLatitude: command.FromLatitude,
            fromLongitude: command.FromLongitude,
            toLatitude: command.ToLatitude,
            toLongitude: command.ToLongitude,
            description: command.Description,
            weightGrams: command.WeightGrams,
            costCents: command.CostCents,
            currency: command.Currency,
            courierNote: command.CourierNote,
            items: command.Items?.Select(i => new OrderItem(
                    i.ProductId, i.Name, i.PriceCents, i.Quantity))
                .ToList()
        );
    }
}
