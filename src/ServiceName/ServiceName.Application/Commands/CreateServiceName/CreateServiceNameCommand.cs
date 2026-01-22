using MediatR;
using ServiceName.Application.Models;

namespace ServiceName.Application.Commands.CreateServiceName;

public record CreateServiceNameCommand(CreateServiceNameModel Model) : IRequest<ServiceNameView>;
