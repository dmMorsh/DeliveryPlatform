using CourierService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CourierService.Application.Commands.RegisterCourier;

public record RegisterCourierCommand(
    string FullName,
    string Phone,
    string Email,
    string DocumentNumber
) : IRequest<ApiResponse<CourierView>>;
