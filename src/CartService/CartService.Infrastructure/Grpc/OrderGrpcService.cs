using CartService.Application.Interfaces;
using CartService.Application.Models;
using CartService.Domain.Aggregates;
using Shared.Proto;

namespace CartService.Infrastructure.Grpc;

public class OrderGrpcService : IOrderService
{
    private readonly OrderGrpc.OrderGrpcClient _client;

    public OrderGrpcService(OrderGrpc.OrderGrpcClient client)
    {
        _client = client;
    }
    
    public async Task<Guid> CreateOrderFromCartAsync(Cart cart, CheckoutCartModel model, CancellationToken ct)
    {
        var request = new CreateOrderRequest
        {
            CustomerId = cart.CustomerId.ToString(),
            // CostCents = cart.Items.Select(it => it.PriceCents * it.Quantity).Sum(),
            CostCents = model.CostCents,
            Currency = model.Currency ?? string.Empty,
            FromAddress =  model.FromAddress,
            FromLatitude = model.FromLatitude,
            FromLongitude = model.FromLongitude,
            ToAddress = model.ToAddress,
            ToLatitude = model.ToLatitude,
            ToLongitude = model.ToLongitude,
            WeightGrams = model.WeightGrams,
            CourierNote = model.CourierNote ?? string.Empty,
        };

        request.Items.AddRange(
            cart.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId.ToString(),
                Quantity = i.Quantity,
                Name = i.Name,
                PriceCents = i.PriceCents,
            })
        );

        var response = await _client.CreateOrderAsync(request, cancellationToken: ct);

        return Guid.Parse(response.OrderId);
    }
}