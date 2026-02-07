using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;

namespace PaymentService.Application.Commands.CreatePayment;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, PaymentView>
{
    private readonly IPaymentRepository _repo;
    private readonly IUnitOfWork _uow;

    public CreatePaymentCommandHandler(IPaymentRepository repo, IUnitOfWork uow)
    {
        _repo = repo;
        _uow = uow;
    }

    public async Task<PaymentView> Handle(CreatePaymentCommand request, CancellationToken ct)
    {
        var payment = Payment.Create(request.Model.OrderId, request.Model.Amount, request.Model.Currency);
        try
        {
            //_db.Payments.Add(payment);
            //await _db.SaveChangesAsync(ct);
            await _repo.AddAsync(payment, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            payment = await _repo.GetByIdAsync(request.Model.OrderId, ct);
                //.SingleAsync(p => p.OrderId == orderId && p.Status == PaymentStatus.Pending, ct);
        }
        return new PaymentView(payment.Id, payment.AmountCents, payment.CreatedAt);
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg
               && pg.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
