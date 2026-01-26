using CourierService.Application.Interfaces;
using MediatR;
using Mapster;
using Shared.Utilities;
using CourierService.Domain.Aggregates;
using CourierService.Application.Mapping;
using CourierService.Application.Models;
using Microsoft.Extensions.Logging;

namespace CourierService.Application.Commands.RegisterCourier;

public class RegisterCourierCommandHandler : IRequestHandler<RegisterCourierCommand, ApiResponse<CourierView>>
{
    private readonly ICourierRepository _repository;
    private readonly IUnitOfWork _uow;
    private readonly ICourierEventMapper _eventMapper;
    private readonly ILogger<RegisterCourierCommandHandler> _logger;

    public RegisterCourierCommandHandler(
        ICourierRepository repository,
        IUnitOfWork uow,
        ICourierEventMapper eventMapper,
        ILogger<RegisterCourierCommandHandler> logger)
    {
        _repository = repository;
        _uow = uow;
        _eventMapper = eventMapper;
        _logger = logger;
    }

    public async Task<ApiResponse<CourierView>> Handle(RegisterCourierCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var dto = request.Model;

            if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
                return ApiResponse<CourierView>.ErrorResponse("Name and phone are required");

            var existingCourier = await _repository.GetCourierByPhoneAsync(dto.Phone);
            if (existingCourier != null)
                return ApiResponse<CourierView>.ErrorResponse("Courier with this phone already exists");

            var courier = Courier.Register(dto.FullName, dto.Phone, dto.Email, dto.DocumentNumber);

            var created = await _repository.CreateCourierAsync(courier);
            _logger.LogInformation("Courier created: {CourierName} (ID: {CourierId})", created.FullName, created.Id);

            // Map domain events to integration events and stage to outbox
            var outboxMessages = created.DomainEvents
                .Select(de =>
                {
                    var ie = _eventMapper.MapFromDomainEvent(de);
                    return OutboxMessage.From(ie!);
                })
                .ToList();
            
            // Commit aggregate atomically
            await _uow.SaveChangesAsync(outboxMessages, cancellationToken);
            created.ClearDomainEvents();

            var result = created.Adapt<CourierView>();
            result.Status = (int)created.Status;
            return ApiResponse<CourierView>.SuccessResponse(result, "Courier created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating courier");
            return ApiResponse<CourierView>.ErrorResponse("Internal server error");
        }
    }
}
