using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;
using Shared.Utilities;

namespace PaymentService.Application.Commands.CreatePayment;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IPaymentIntegrationEventMapper _eventMapper;

    public CreatePaymentCommandHandler(
        IUnitOfWorkFactory factory,
        IPaymentIntegrationEventMapper eventMapper)
    {
        _factory = factory;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(CreatePaymentCommand request, CancellationToken ct)
    {
        var payment = Payment.Create(request.Model.OrderId, request.Model.Amount, request.Model.Currency);
        var outbox = new List<OutboxMessage>();
        try
        {
            await using var uow = _factory.Create(request.Model.OrderId);
            await uow.Payments.AddAsync(payment, ct);
            outbox.Add(OutboxMessage.From(_eventMapper.MapCreated(payment)));
            await uow.SaveChangesAsync(outbox, ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await using var uow = _factory.Create(request.Model.OrderId);
            payment = await uow.Payments.GetByOrderId(request.Model.OrderId, ct);
        }

        return ApiResponse.SuccessResponse();
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg
               && pg.SqlState == PostgresErrorCodes.UniqueViolation;
    }
}
