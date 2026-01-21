using CourierService.Repositories;
using CourierService.Data;
using CourierService.Domain.Aggregates;
using Shared.Utilities;
using Shared.Services;
using Shared.Contracts.Events;
using Shared.Contracts;
using Mapster;

namespace CourierService.Services;

public class CourierApplicationService
{
    private readonly ICourierRepository _repository;
    private readonly IEventProducer _eventProducer;
    private readonly ILogger<CourierApplicationService> _logger;
    private readonly ICourierIntegrationEventMapper _eventMapper;
    private readonly IUnitOfWork _uow;
    // private readonly IOutboxRepository _outboxRepository;

    public CourierApplicationService(ICourierRepository repository, IEventProducer eventProducer, ILogger<CourierApplicationService> logger, ICourierIntegrationEventMapper eventMapper, IUnitOfWork uow
        // , IOutboxRepository outboxRepository
        )
    {
        _repository = repository;
        _eventProducer = eventProducer;
        _logger = logger;
        _eventMapper = eventMapper;
        _uow = uow;
        // _outboxRepository = outboxRepository;
    }

    public async Task<ApiResponse<CourierDto>> GetCourierAsync(Guid courierId)
    {
        try
        {
            var courier = await _repository.GetCourierByIdAsync(courierId);
            if (courier == null)
                return ApiResponse<CourierDto>.ErrorResponse($"Courier {courierId} not found");

            return ApiResponse<CourierDto>.SuccessResponse(MapToDto(courier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting courier {CourierId}", courierId);
            return ApiResponse<CourierDto>.ErrorResponse("Internal server error");
        }
    }

    public async Task<ApiResponse<CourierDto>> CreateCourierAsync(CreateCourierDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
                return ApiResponse<CourierDto>.ErrorResponse("Name and phone are required");

            var existingCourier = await _repository.GetCourierByPhoneAsync(dto.Phone);
            if (existingCourier != null)
                return ApiResponse<CourierDto>.ErrorResponse("Courier with this phone already exists");

            var courier = Courier.Register(dto.FullName, dto.Phone, dto.Email, dto.DocumentNumber);

            var created = await _repository.CreateCourierAsync(courier);
            _logger.LogInformation("Courier created: {CourierName} (ID: {CourierId})", created.FullName, created.Id);

            // Map domain events to integration events (explicit, no magic) and stage to outbox
            foreach (var de in created.DomainEvents)
            {
                var ie = _eventMapper.MapFromDomainEvent(de);
                if (ie == null) continue;

                // _outboxRepository.Add(new Infrastructure.OutboxMessage
                // {
                //     Id = Guid.NewGuid(),
                //     AggregateId = ie.AggregateId,
                //     Type = ie.EventType,
                //     Payload = EventSerializer.SerializeEvent(ie),
                //     OccurredAt = ie.Timestamp
                // });
            }
            created.ClearDomainEvents();

            // Commit aggregate and outbox atomically
            await _uow.SaveChangesAsync();

            return ApiResponse<CourierDto>.SuccessResponse(MapToDto(created), "Courier created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating courier");
            return ApiResponse<CourierDto>.ErrorResponse("Internal server error");
        }
    }

    public async Task<ApiResponse<CourierDto>> UpdateCourierAsync(Guid courierId, UpdateCourierDto dto)
    {
        try
        {
            var courier = await _repository.GetCourierByIdAsync(courierId);
            if (courier == null)
                return ApiResponse<CourierDto>.ErrorResponse($"Courier {courierId} not found");

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

            _logger.LogInformation("Courier {CourierId} updated: {OldStatus} -> {NewStatus}", courierId, oldStatus, courier.Status);

            foreach (var de in courier.DomainEvents)
            {
                var ie = _eventMapper.MapFromDomainEvent(de);
                if (ie == null) continue;

                // _outboxRepository.Add(new Infrastructure.OutboxMessage
                // {
                //     Id = Guid.NewGuid(),
                //     AggregateId = ie.AggregateId,
                //     Type = ie.EventType,
                //     Payload = EventSerializer.SerializeEvent(ie),
                //     OccurredAt = ie.Timestamp
                // });
            }
            courier.ClearDomainEvents();

            await _uow.SaveChangesAsync();

            return ApiResponse<CourierDto>.SuccessResponse(MapToDto(updated), "Courier updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating courier {CourierId}", courierId);
            return ApiResponse<CourierDto>.ErrorResponse("Internal server error");
        }
    }

    public async Task<ApiResponse<List<CourierDto>>> GetActiveCouriersAsync()
    {
        try
        {
            var couriers = await _repository.GetActiveCouriersAsync();
            var dtos = couriers.Select(MapToDto).ToList();
            return ApiResponse<List<CourierDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active couriers");
            return ApiResponse<List<CourierDto>>.ErrorResponse("Internal server error");
        }
    }

    private static CourierDto MapToDto(Courier courier)
    {
        var dto = courier.Adapt<CourierDto>();
        dto.Status = (int)courier.Status;
        return dto;
    }
}
