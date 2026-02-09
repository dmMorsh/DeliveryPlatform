using MediatR;
using PaymentService.Application.Models;
using Shared.Utilities;

namespace PaymentService.Application.Commands.ProcessSberbankWebhook;

public record ProcessSberbankWebhookCommand(SberbankWebhookModel Model) : IRequest<ApiResponse>;
