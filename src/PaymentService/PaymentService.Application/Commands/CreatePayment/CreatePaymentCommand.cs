using MediatR;
using Shared.Utilities;

namespace PaymentService.Application.Commands.CreatePayment;

public record CreatePaymentCommand(Guid OrderId, long Amount, string Currency) : IRequest<ApiResponse>;
