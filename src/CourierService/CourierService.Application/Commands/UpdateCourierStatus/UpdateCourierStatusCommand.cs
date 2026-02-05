using CourierService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CourierService.Application.Commands.UpdateCourierStatus;

public record UpdateCourierStatusCommand(Guid CourierId, UpdateCourierModel Model) : IRequest<ApiResponse<CourierView>>;
