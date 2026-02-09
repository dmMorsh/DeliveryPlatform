using MediatR;
using Shared.Utilities;

namespace PaymentService.Application.Commands.CancelPayment;

public record CancelPaymentCommand(Guid OrderId) : IRequest<ApiResponse>;
