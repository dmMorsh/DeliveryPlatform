using MediatR;
using Shared.Contracts;

namespace PaymentService.Application.Commands.ProcessOrderCanceled;

public record ProcessOrderCanceledCommand(Guid OrderId) : IRequest<ApiResponse>;
