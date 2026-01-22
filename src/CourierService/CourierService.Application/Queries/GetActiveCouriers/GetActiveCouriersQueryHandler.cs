using MediatR;
using Mapster;
using Shared.Contracts;
using Shared.Utilities;
using CourierService.Repositories;
using Microsoft.Extensions.Logging;

namespace CourierService.Application.Queries.GetActiveCouriers;

public class GetActiveCouriersQueryHandler : IRequestHandler<GetActiveCouriersQuery, ApiResponse<List<CourierDto>>>
{
    private readonly ICourierRepository _repository;
    private readonly ILogger<GetActiveCouriersQueryHandler> _logger;

    public GetActiveCouriersQueryHandler(ICourierRepository repository, ILogger<GetActiveCouriersQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CourierDto>>> Handle(GetActiveCouriersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var couriers = await _repository.GetActiveCouriersAsync();
            var dtos = couriers.Select(c =>
            {
                var dto = c.Adapt<CourierDto>();
                dto.Status = (int)c.Status;
                return dto;
            }).ToList();
            return ApiResponse<List<CourierDto>>.SuccessResponse(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active couriers");
            return ApiResponse<List<CourierDto>>.ErrorResponse("Internal server error");
        }
    }
}
