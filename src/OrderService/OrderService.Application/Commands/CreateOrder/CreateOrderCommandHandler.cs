using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Interfaces;
using OrderService.Domain;
using Shared.Utilities;
using Mapster;
using OrderService.Application.Models;

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

        //пока пустые, потом доделаем
        var items = new List<OrderItem>();

        var order = Mapping.OrderFactory.CreateNew(
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
            courierNote: createModel.CourierNote,
            items: items
        );

        await _repository.CreateOrderAsync(order, ct);
        
        var outboxMessages = order.DomainEvents
            .Select(de =>
            {
                var ie = _eventMapper.MapFromDomainEvent(de);
                return OutboxMessage.From(ie!);
            })
            .ToList();

        await _uow.SaveChangesAsync(outboxMessages, ct);
        order.ClearDomainEvents();

        _logger.LogInformation("Order created: {OrderNumber} (ID: {OrderId})", order.OrderNumber, order.Id);

        return ApiResponse<OrderView>.SuccessResponse(order.Adapt<OrderView>(), "Order created successfully");
    }
}
