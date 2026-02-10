using DeliveryService.Application.Interfaces;
using DeliveryService.Application.Mapping;
using DeliveryService.Application.Models;
using DeliveryService.Domain.Aggregates;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Utilities;

namespace DeliveryService.Application.Commands.CreateDeliveryFromOrder;

public class CreateDeliveryFromOrderCommandHandler : IRequestHandler<CreateDeliveryFromOrderCommand, ApiResponse>
{
    private readonly IDeliveryRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly IDeliveryEventMapper _eventMapper;
    private readonly ILogger<CreateDeliveryFromOrderCommandHandler> _logger;

    public CreateDeliveryFromOrderCommandHandler(
        IDeliveryRepository repository,
        IUnitOfWork uow,
        IDeliveryEventMapper eventMapper,
        ILogger<CreateDeliveryFromOrderCommandHandler> logger)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse> Handle(CreateDeliveryFromOrderCommand request, CancellationToken ct)
    {
        var existing = await _repository.GetByOrderIdAsync(request.OrderId, ct);
        if (existing != null)
            return ApiResponse.SuccessResponse();

        var delivery = Delivery.CreateFromOrder(
            request.OrderId,
            request.ClientId,
            request.FromAddress,
            request.ToAddress,
            request.FromLatitude,
            request.FromLongitude,
            request.ToLatitude,
            request.ToLongitude);

        await _repository.AddAsync(delivery, ct);

        var outbox = delivery.DomainEvents
            .Select(_eventMapper.MapFromDomainEvent)
            .Where(e => e != null)
            .Select(OutboxMessage.From!)
            .ToList();

        await _uow.SaveChangesAsync(outbox, ct);
        delivery.ClearDomainEvents();

        _logger.LogInformation("Delivery created for order {OrderId} => {DeliveryId}", request.OrderId, delivery.Id);
        return ApiResponse.SuccessResponse();
    }
}
