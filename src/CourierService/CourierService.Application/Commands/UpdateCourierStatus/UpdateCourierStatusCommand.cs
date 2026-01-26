using CourierService.Application.Models;
using Shared.Utilities;
using MediatR;

namespace CourierService.Application.Commands.UpdateCourierStatus;

public record UpdateCourierStatusCommand(Guid CourierId, UpdateCourierModel Model) : IRequest<ApiResponse<CourierView>>;
