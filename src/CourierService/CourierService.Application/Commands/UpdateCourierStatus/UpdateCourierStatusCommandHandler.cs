using CourierService.Application.Interfaces;
using MediatR;
using Mapster;
using Shared.Utilities;
using CourierService.Domain.Aggregates;
using CourierService.Application.Mapping;
using CourierService.Application.Models;
using CourierService.Repositories;
using Microsoft.Extensions.Logging;

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

            var dto = request.Model;
            var oldStatus = courier.Status;

            if (dto.Status.HasValue && Enum.IsDefined(typeof(CourierStatus), dto.Status.Value))
                courier.ChangeStatus((CourierStatus)dto.Status.Value);

            if (dto.CurrentLatitude.HasValue && dto.CurrentLongitude.HasValue)
            {
                courier.UpdateLocation(dto.CurrentLatitude.Value, dto.CurrentLongitude.Value);
            }

            if (dto.IsActive.HasValue)
                if (!dto.IsActive.Value)
                    courier.Deactivate();

            var updated = await _repository.UpdateCourierAsync(courier);
            if (updated == null)
                return ApiResponse<CourierView>.ErrorResponse("Failed to update courier");

            _logger.LogInformation("Courier {CourierId} updated: {OldStatus} -> {NewStatus}", request.CourierId, oldStatus, courier.Status);

            // Map domain events to integration events and stage to outbox
            var outboxMessages = updated.DomainEvents
                .Select(de =>
                {
                    var ie = _eventMapper.MapFromDomainEvent(de);
                    return OutboxMessage.From(ie!);
                })
                .ToList();

            // Add status changed event if status was modified
            if (dto.Status.HasValue && oldStatus != updated.Status)
            {
                var statusChangeEvent = _eventMapper.MapCourierStatusChangedEvent(
                    updated.Id,
                    (int)oldStatus,
                    (int)updated.Status);
                outboxMessages.Add(OutboxMessage.From(statusChangeEvent));
            }

            // Add location updated event if location was provided
            if (dto.CurrentLatitude.HasValue && dto.CurrentLongitude.HasValue)
            {
                var locationEvent = _eventMapper.MapLocationUpdatedEvent(
                    updated.Id,
                    dto.CurrentLatitude.Value,
                    dto.CurrentLongitude.Value);
                outboxMessages.Add(OutboxMessage.From(locationEvent));
            }
            
            // Commit aggregate atomically
            await _uow.SaveChangesAsync(outboxMessages, cancellationToken);
            updated.ClearDomainEvents();

            var result = updated.Adapt<CourierView>();
            result.Status = (int)updated.Status;
            return ApiResponse<CourierView>.SuccessResponse(result, "Courier updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating courier {CourierId}", request.CourierId);
            return ApiResponse<CourierView>.ErrorResponse("Internal server error");
        }
    }
}
