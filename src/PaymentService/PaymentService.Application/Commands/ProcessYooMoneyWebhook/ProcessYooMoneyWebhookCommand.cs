using MediatR;
using PaymentService.Application.Models;
using Shared.Utilities;

namespace PaymentService.Application.Commands.ProcessYooMoneyWebhook;

public record ProcessYooMoneyWebhookCommand(YooMoneyWebhookModel Model) : IRequest<ApiResponse>;
