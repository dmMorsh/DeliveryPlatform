using CourierService.Application.Interfaces;
using CourierService.Application.Models;
using Mapster;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Utilities;

namespace CourierService.Application.Queries.GetCourier;

public class GetCourierQueryHandler : IRequestHandler<GetCourierQuery, ApiResponse<CourierView>>
{
    private readonly ICourierRepository _repository;
    private readonly ILogger<GetCourierQueryHandler> _logger;

    public GetCourierQueryHandler(ICourierRepository repository, ILogger<GetCourierQueryHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<ApiResponse<CourierView>> Handle(GetCourierQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var courier = await _repository.GetCourierByIdAsync(request.CourierId);
            if (courier == null)
                return ApiResponse<CourierView>.ErrorResponse($"Courier {request.CourierId} not found");

            var result = courier.Adapt<CourierView>();
            result.Status = (int)courier.Status;
            return ApiResponse<CourierView>.SuccessResponse(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting courier {CourierId}", request.CourierId);
            return ApiResponse<CourierView>.ErrorResponse("Internal server error");
        }
    }
}
