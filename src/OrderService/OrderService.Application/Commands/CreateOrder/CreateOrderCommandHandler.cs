using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using Shared.Utilities;
using Mapster;
using OrderService.Application.Models;
using OrderService.Application.Utils;
using OrderService.Domain.Events;

namespace OrderService.Application.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ApiResponse<OrderView>>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IOrderIntegrationEventMapper _eventMapper;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(IOrderRepository repository, IUnitOfWork uow, IOrderIntegrationEventMapper eventMapper, ILogger<CreateOrderCommandHandler> logger)
    {
        _repository = repository;
        _uow = uow;
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

        await _repository.CreateOrderAsync(order, ct);
        
        var createdDomainEvent = order.DomainEvents.First() as OrderCreatedDomainEvent;
        if (createdDomainEvent == null) throw new AggregateException("ERROR !!!");
        
        var shardGroups = createdDomainEvent.Items
            .GroupBy(i => ShardingHelper.ShardForProduct(i.ProductId));
        var outboxMessages = new List<OutboxMessage>();
        
        foreach (var shardGroup in shardGroups)
        {
            var shardId = shardGroup.Key;
            var shardEvent = _eventMapper.MapFromOrderCreatedDomainEvent(createdDomainEvent, shardGroup.ToArray(), shardId);
            if (shardEvent != null) outboxMessages.Add(OutboxMessage.From(shardEvent));
        }
        
        await _uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();

        _logger.LogInformation("Order created: {OrderNumber} (ID: {OrderId})", order.OrderNumber, order.Id);

        return ApiResponse<OrderView>.SuccessResponse(order.Adapt<OrderView>(), "Order created successfully");
    }
}