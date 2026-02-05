using CourierService.Application.Models;
using MediatR;
using Shared.Utilities;

namespace CourierService.Application.Commands.RegisterCourier;

public record RegisterCourierCommand(CreateCourierModel Model) : IRequest<ApiResponse<CourierView>>;
