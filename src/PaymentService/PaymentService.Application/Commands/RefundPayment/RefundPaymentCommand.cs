using MediatR;
using Shared.Contracts;

namespace PaymentService.Application.Commands.RefundPayment;

public record RefundPaymentCommand(Guid OrderId, long AmountCents) : IRequest<ApiResponse>;
