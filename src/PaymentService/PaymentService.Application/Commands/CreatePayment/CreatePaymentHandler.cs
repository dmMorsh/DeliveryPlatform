using MediatR;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;

namespace PaymentService.Application.Commands.CreatePayment;

public class CreatePaymentHandler : IRequestHandler<CreatePaymentCommand, PaymentView>
{
    private readonly IPaymentRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreatePaymentHandler(IPaymentRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<PaymentView> Handle(CreatePaymentCommand request, CancellationToken ct)
    {
        var entity = Payment.Create(request.Model.Name);
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return new PaymentView(entity.Id, entity.Name, entity.CreatedAt);
    }
}
