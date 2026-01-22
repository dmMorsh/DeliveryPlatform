using CourierService.Application.Interfaces;
using MediatR;
using Mapster;
using Shared.Contracts;
using Shared.Utilities;
using CourierService.Domain.Aggregates;
using CourierService.Application.Mapping;
using CourierService.Application.Models;
using CourierService.Repositories;
using Microsoft.Extensions.Logging;

namespace CourierService.Application.Commands.UpdateCourierStatus;

public class UpdateCourierStatusCommandHandler : IRequestHandler<UpdateCourierStatusCommand, ApiResponse<CourierDto>>
{
    private readonly ICourierRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly ICourierIntegrationEventMapper _eventMapper;
    private readonly ILogger<UpdateCourierStatusCommandHandler> _logger;

    public UpdateCourierStatusCommandHandler(
        ICourierRepository repository,
        IUnitOfWork uow,
        ICourierIntegrationEventMapper eventMapper,
        ILogger<UpdateCourierStatusCommandHandler> logger)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse<CourierDto>> Handle(UpdateCourierStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var courier = await _repository.GetCourierByIdAsync(request.CourierId);
            if (courier == null)
                return ApiResponse<CourierDto>.ErrorResponse($"Courier {request.CourierId} not found");

            var dto = request.Dto;
            var oldStatus = courier.Status;

            if (dto.Status.HasValue && Enum.IsDefined(typeof(CourierStatus), dto.Status.Value))
                courier.ChangeStatus((CourierStatus)dto.Status.Value);

            if (dto.CurrentLatitude.HasValue && dto.CurrentLongitude.HasValue)
            {
                courier.UpdateLocation(dto.CurrentLatitude.Value, dto.CurrentLongitude.Value);
            }

            if (dto.IsActive.HasValue)
                courier.IsActive = dto.IsActive.Value;

            var updated = await _repository.UpdateCourierAsync(courier);
            if (updated == null)
                return ApiResponse<CourierDto>.ErrorResponse("Failed to update courier");

            _logger.LogInformation("Courier {CourierId} updated: {OldStatus} -> {NewStatus}", request.CourierId, oldStatus, courier.Status);

            // Map domain events to integration events and stage to outbox
            var outboxMessages = updated.DomainEvents
                .Select(de =>
                {
                    var ie = _eventMapper.MapFromDomainEvent(de);
                    return OutboxMessage.From(ie!);
                })
                .ToList();
            
            // Commit aggregate atomically
            await _uow.SaveChangesAsync(outboxMessages, cancellationToken);
            updated.ClearDomainEvents();

            var result = updated.Adapt<CourierDto>();
            result.Status = (int)updated.Status;
            return ApiResponse<CourierDto>.SuccessResponse(result, "Courier updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating courier {CourierId}", request.CourierId);
            return ApiResponse<CourierDto>.ErrorResponse("Internal server error");
        }
    }
}
