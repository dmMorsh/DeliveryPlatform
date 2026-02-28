using MediatR;
using Shared.Utilities;

namespace OrderService.Application.Commands.MarkOrderRejected;

public record MarkOrderRejectedCommand(Guid OrderId, string? Reason) : IRequest<ApiResponse>;
