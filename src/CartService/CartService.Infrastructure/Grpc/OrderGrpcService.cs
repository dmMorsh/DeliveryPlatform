using CartService.Application.Commands.Checkout;
using CartService.Application.Interfaces;
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
    
    public async Task<Guid> CreateOrderFromCartAsync(Cart cart, CheckoutCartCommand command, CancellationToken ct)
    {
        var request = new CreateOrderRequest
        {
            CustomerId = cart.CustomerId.ToString(),
            CostCents = command.CostCents,
            Currency = command.Currency ?? string.Empty,
            FromAddress = command.FromAddress,
            FromLatitude = command.FromLatitude,
            FromLongitude = command.FromLongitude,
            ToAddress = command.ToAddress,
            ToLatitude = command.ToLatitude,
            ToLongitude = command.ToLongitude,
            WeightGrams = command.WeightGrams,
            CourierNote = command.CourierNote ?? string.Empty,
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