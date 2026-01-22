using MediatR;
using ServiceName.Application.Interfaces;
using ServiceName.Application.Models;
using ServiceName.Domain.Entities;

namespace ServiceName.Application.Commands.CreateServiceName;

public class CreateServiceNameHandler : IRequestHandler<CreateServiceNameCommand, ServiceNameView>
{
    private readonly IServiceNameRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreateServiceNameHandler(IServiceNameRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<ServiceNameView> Handle(CreateServiceNameCommand request, CancellationToken ct)
    {
        var entity = ServiceAggregate.Create(request.Model.Name);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return new ServiceNameView(entity.Id, entity.Name, entity.CreatedAt);
    }
}
