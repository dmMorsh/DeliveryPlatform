using CartService.Api.Services;
using CartService.Application.Interfaces;
using CartService.Application.Models;
using CartService.Domain.Aggregates;
using CartService.Domain.Entities;
using CartService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Shared.Proto;

namespace CartService.Api.Controllers;

[ApiController]
[Route("cart")]
public class CartController : ControllerBase
{
    private readonly CartDbContext _db;
    private readonly OrderGrpc.OrderGrpcClient _orderClient;
    private readonly ICartRepository _repo;
    private readonly Shared.Services.IEventProducer _producer;
    private readonly ICartIntegrationEventMapper _mapper;

    public CartController(CartDbContext db, OrderGrpc.OrderGrpcClient orderClient, ICartRepository repo, Shared.Services.IEventProducer producer, ICartIntegrationEventMapper mapper)
    {
        _db = db;
        _orderClient = orderClient;
        _repo = repo;
        _producer = producer;
        _mapper = mapper;
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem()
    {
        var customerId = Guid.NewGuid(); // временно

        var cart = await _repo.GetCartByCustomerIdAsync(customerId) ?? new Cart(customerId);

        cart.AddItem(new CartItem(
            Guid.NewGuid(),
            "Burger",
            350,
            1));

        await _repo.CreateOrUpdateAsync(cart);

        // persist domain events to outbox for reliable delivery
        foreach (var de in cart.DomainEvents)
        {
            var ie = _mapper.MapFromDomainEvent(de);
            if (ie == null) continue;

            _db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = ie.AggregateId,
                Type = ie.EventType,
                Payload = Shared.Utilities.EventSerializer.SerializeEvent(ie),
                OccurredAt = ie.Timestamp
            });
        }
        await _db.SaveChangesAsync();
        cart.ClearDomainEvents();

        return Ok();
    }
    
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        var customerId = Guid.NewGuid(); // временно, вместо Identity

        var cart = await _repo.GetCartByCustomerIdAsync(customerId);

        if (cart == null || !cart.Items.Any())
            return BadRequest("Cart is empty");

        var grpcRequest = new CreateOrderRequest
        {
            CustomerId = customerId.ToString()
        };
        
        grpcRequest.Items.AddRange(cart.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId.ToString(),
            Name = i.Name,
            Price = i.Price,
            Quantity = i.Quantity
        }));

        var response = await _orderClient.CreateOrderAsync(grpcRequest);

        // trigger checkout on aggregate
        cart.Checkout();
        await _repo.CreateOrUpdateAsync(cart);

        // persist checkout event(s) to outbox
        foreach (var de in cart.DomainEvents)
        {
            var ie = _mapper.MapFromDomainEvent(de);
            if (ie == null) continue;

            _db.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = ie.AggregateId,
                Type = ie.EventType,
                Payload = Shared.Utilities.EventSerializer.SerializeEvent(ie),
                OccurredAt = ie.Timestamp
            });
        }
        await _db.SaveChangesAsync();
        cart.ClearDomainEvents();

        return Ok(new
        {
            orderId = response.OrderId
        });
    }

}
