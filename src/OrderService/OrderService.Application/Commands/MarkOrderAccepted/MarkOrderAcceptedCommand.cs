using MediatR;
using OrderService.Application.Models;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkOrderAccepted;

public record MarkOrderAcceptedCommand(Guid OrderId) : IRequest<ApiResponse>;
