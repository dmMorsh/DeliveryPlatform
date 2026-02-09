using MediatR;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using PaymentService.Domain.Aggregates;
using Shared.Utilities;

namespace PaymentService.Application.Commands.CancelPayment;

public class CancelPaymentCommandHandler : IRequestHandler<CancelPaymentCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;
    private readonly IPaymentProviderResolver _providers;
    private readonly IPaymentIntegrationEventMapper _eventMapper;

    public CancelPaymentCommandHandler(
        IUnitOfWorkFactory factory,
        IPaymentProviderResolver providers,
        IPaymentIntegrationEventMapper eventMapper)
    {
        _factory = factory;
        _providers = providers;
        _eventMapper = eventMapper;
    }

    public async Task<ApiResponse> Handle(CancelPaymentCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var payment = await uow.Payments.GetByOrderId(request.OrderId, ct);
        if (payment is null)
            return ApiResponse.ErrorResponse("Payment not found");

        if (payment.Status is PaymentStatus.Captured or PaymentStatus.Refunded)
            return ApiResponse.ErrorResponse("Payment status is not valid");

        var provider = _providers.Get(payment.Provider);
        await provider.CancelPayment(payment.ExternalPaymentId, ct);

        var prev = payment.Status;
        payment.MarkCancelled();
        var outbox = new List<OutboxMessage>();
        if (payment.Status != prev)
            outbox.Add(OutboxMessage.From(_eventMapper.MapCancelled(payment)));

        await uow.Payments.UpsertExternalPaymentIdMap(payment.OrderId, payment.Id, payment.ExternalPaymentId, payment.Provider, ct);
        await uow.SaveChangesAsync(outbox, ct);

        return ApiResponse.SuccessResponse();
    }
}
