using MediatR;
using PaymentService.Application.Models;
using Shared.Utilities;

namespace PaymentService.Application.Commands.CreatePayment;

public record CreatePaymentCommand(CreatePaymentModel Model) : IRequest<ApiResponse>;
