using CourierService.Application.Interfaces;
using CourierService.Application.Models;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Utilities;

namespace CourierService.Application.Queries.GetActiveCouriers;

public class GetActiveCouriersQueryHandler : IRequestHandler<GetActiveCouriersQuery, ApiResponse<List<CourierView>>>
{
    private readonly ICourierRepository _repository;
    private readonly ILogger<GetActiveCouriersQueryHandler> _logger;

    public GetActiveCouriersQueryHandler(ICourierRepository repository, ILogger<GetActiveCouriersQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ApiResponse<List<CourierView>>> Handle(GetActiveCouriersQuery request, CancellationToken ct)
    {
        try
        {
            var couriers = await _repository.GetActiveCouriersAsync(ct);
            var views = couriers.Select(c =>
            {
                var dto = c.Adapt<CourierView>();
                dto.Status = (int)c.Status;
                return dto;
            }).ToList();
            return ApiResponse<List<CourierView>>.SuccessResponse(views);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active couriers");
            return ApiResponse<List<CourierView>>.ErrorResponse("Internal server error");
        }
    }
}
