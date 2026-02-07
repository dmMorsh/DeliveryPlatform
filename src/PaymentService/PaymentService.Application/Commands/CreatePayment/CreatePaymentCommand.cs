using MediatR;
using PaymentService.Application.Models;

namespace PaymentService.Application.Commands.CreatePayment;

public record CreatePaymentCommand(CreatePaymentModel Model) : IRequest<PaymentView>;
