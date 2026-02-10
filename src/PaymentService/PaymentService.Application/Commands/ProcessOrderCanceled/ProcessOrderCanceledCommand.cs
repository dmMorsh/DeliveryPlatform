using MediatR;
using Shared.Utilities;

namespace PaymentService.Application.Commands.ProcessOrderCanceled;

public record ProcessOrderCanceledCommand(Guid OrderId) : IRequest<ApiResponse>;
