using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Application.Models;
using Shared.Utilities;

namespace OrderService.Application.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ApiResponse<OrderView>>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        IUnitOfWorkFactory factory,
        IOrderIntegrationEventMapper eventMapper, 
        ILogger<CreateOrderCommandHandler> logger)
    {
        _factory = factory;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderView>> Handle(CreateOrderCommand request, CancellationToken ct)
    {
        var createModel = request.CreateModel;

        if (string.IsNullOrWhiteSpace(createModel.FromAddress) || string.IsNullOrWhiteSpace(createModel.ToAddress))
            return ApiResponse<OrderView>.ErrorResponse("From and To addresses are required");

        if (createModel.CostCents <= 0)
            return ApiResponse<OrderView>.ErrorResponse("Cost must be greater than 0");
        
        var order = Mapping.OrderFactory.CreateNew(createModel);

        await using var uow = _factory.Create(order.Id);
        
        await uow.Orders.CreateOrderAsync(order, ct);
        
        var outboxMessages = order.DomainEvents
            .Select(de => _eventMapper.MapFromDomainEvent(de))
            .Where(ie => ie != null)
            .Select(ie => OutboxMessage.From(ie!))
            .ToList();
        
        await uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();

        _logger.LogInformation("Order created: {OrderNumber} (ID: {OrderId})", order.OrderNumber, order.Id);

        return ApiResponse<OrderView>.SuccessResponse(order.Adapt<OrderView>(), "Order created successfully");
    }
}