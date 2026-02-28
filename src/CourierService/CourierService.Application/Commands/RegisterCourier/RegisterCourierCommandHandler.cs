using CourierService.Application.Interfaces;
using CourierService.Application.Models;
using CourierService.Application.Services;
using CourierService.Domain.Aggregates;
using Mapster;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Utilities;

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
            if (string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Phone))
                return ApiResponse<CourierView>.ErrorResponse("Name and phone are required");

            var existingCourier = await _repository.GetCourierByPhoneAsync(request.Phone, cancellationToken);
            if (existingCourier != null)
                return ApiResponse<CourierView>.ErrorResponse("Courier with this phone already exists");

            var courier = Courier.Register(request.FullName, request.Phone, request.Email, request.DocumentNumber);

            var created = await _repository.CreateCourierAsync(courier, cancellationToken);
            _logger.LogInformation("Courier created: {CourierName} (ID: {CourierId})", created.FullName, created.Id);

            // Map domain events to integration events and stage to outbox
            var outboxMessages = created.DomainEvents
                .Select(_eventMapper.MapFromDomainEvent)
                .Where(ie => ie != null)
                .Select(OutboxMessage.From!)
                .ToList();
            
            // Commit aggregate atomically
            try
            {
                await _uow.SaveChangesAsync(outboxMessages, cancellationToken);
            }
            catch (DbUpdateException)
            {
                var existing = await _repository.GetCourierByPhoneAsync(request.Phone, cancellationToken);
                if (existing != null)
                    return ApiResponse<CourierView>.ErrorResponse("Courier with this phone already exists");
                throw;
            }
            created.ClearDomainEvents();
            CourierReadCache.Invalidate(created.Id);

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
