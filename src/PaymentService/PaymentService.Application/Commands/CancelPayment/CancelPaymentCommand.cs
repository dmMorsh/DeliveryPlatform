using MediatR;
using Shared.Contracts;

namespace PaymentService.Application.Commands.CancelPayment;

public record CancelPaymentCommand(Guid OrderId) : IRequest<ApiResponse>;
