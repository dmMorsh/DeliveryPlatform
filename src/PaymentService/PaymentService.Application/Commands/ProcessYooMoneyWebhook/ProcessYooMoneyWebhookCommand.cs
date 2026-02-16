using System.Text.Json;
using MediatR;
using Shared.Utilities;

namespace PaymentService.Application.Commands.ProcessYooMoneyWebhook;

public record ProcessYooMoneyWebhookCommand(string Event, JsonElement Object) : IRequest<ApiResponse>;
