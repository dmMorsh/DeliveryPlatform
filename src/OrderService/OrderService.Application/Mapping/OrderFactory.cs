using OrderService.Application.Models;
using OrderService.Domain.Aggregates;
using OrderService.Domain.Entities;

namespace OrderService.Application.Mapping;

public static class OrderFactory
{ 
    public static Order CreateNew(CreateOrderModel createModel)
    {
        return Order.Create(
            clientId: createModel.ClientId,
            fromAddress: createModel.FromAddress,
            toAddress: createModel.ToAddress,
            fromLatitude: createModel.FromLatitude,
            fromLongitude: createModel.FromLongitude,
            toLatitude: createModel.ToLatitude,
            toLongitude: createModel.ToLongitude,
            description: createModel.Description,
            weightGrams: createModel.WeightGrams,
            costCents: createModel.CostCents,
            currency: createModel.Currency,
            courierNote: createModel.CourierNote,
            items: createModel.Items?.Select(i => new OrderItem(
                    i.ProductId, i.Name, i.PriceCents, i.Quantity))
                .ToList()
        );
    }
}
