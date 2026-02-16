using CourierService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CourierService.Application.Commands.UpdateCourierStatus;

public record UpdateCourierStatusCommand(
    Guid CourierId,
    int? Status,
    double? CurrentLatitude,
    double? CurrentLongitude,
    bool? IsActive
) : IRequest<ApiResponse<CourierView>>;
