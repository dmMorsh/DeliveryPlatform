using CourierService.Application.Interfaces;
using CourierService.Application.Models;
using CourierService.Domain.Aggregates;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Utilities;

namespace CourierService.Application.Commands.UpdateCourierStatus;

public class UpdateCourierStatusCommandHandler : IRequestHandler<UpdateCourierStatusCommand, ApiResponse<CourierView>>
{
    private readonly ICourierRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly ICourierEventMapper _eventMapper;
    private readonly ILogger<UpdateCourierStatusCommandHandler> _logger;

    public UpdateCourierStatusCommandHandler(
        ICourierRepository repository,
        IUnitOfWork uow,
        ICourierEventMapper eventMapper,
        ILogger<UpdateCourierStatusCommandHandler> logger)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse<CourierView>> Handle(UpdateCourierStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var courier = await _repository.GetCourierByIdAsync(request.CourierId);
            if (courier == null)
                return ApiResponse<CourierView>.ErrorResponse($"Courier {request.CourierId} not found");

            var oldStatus = courier.Status;

            if (request.Status.HasValue && Enum.IsDefined(typeof(CourierStatus), request.Status.Value))
                courier.ChangeStatus((CourierStatus)request.Status.Value);

            if (request.CurrentLatitude.HasValue && request.CurrentLongitude.HasValue)
            {
                courier.UpdateLocation(request.CurrentLatitude.Value, request.CurrentLongitude.Value);
            }

            if (request.IsActive.HasValue)
                if (!request.IsActive.Value)
                    courier.Deactivate();
            
            _logger.LogInformation("Courier {CourierId} updated: {OldStatus} -> {NewStatus}", request.CourierId, oldStatus, courier.Status);

            // Map domain events to integration events and stage to outbox
            var outboxMessages = courier.DomainEvents
                .Select(_eventMapper.MapFromDomainEvent)
                .Where(ie => ie != null)
                .Select(OutboxMessage.From!)
                .ToList();
            
            await _uow.SaveChangesAsync(outboxMessages, cancellationToken);
            courier.ClearDomainEvents();

            var result = courier.Adapt<CourierView>();
            result.Status = (int)courier.Status;
            return ApiResponse<CourierView>.SuccessResponse(result, "Courier updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating courier {CourierId}", request.CourierId);
            return ApiResponse<CourierView>.ErrorResponse("Internal server error");
        }
    }
}
