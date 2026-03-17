using MediatR;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Aggregates;
using Shared.Contracts;

namespace PaymentService.Application.Commands.MarkPaymentReady;

public class MarkPaymentReadyCommandHandler : IRequestHandler<MarkPaymentReadyCommand, ApiResponse>
{
    private readonly IUnitOfWorkFactory _factory;

    public MarkPaymentReadyCommandHandler(IUnitOfWorkFactory factory)
    {
        _factory = factory;
    }

    public async Task<ApiResponse> Handle(MarkPaymentReadyCommand request, CancellationToken ct)
    {
        await using var uow = _factory.Create(request.OrderId);
        var payment = await uow.Payments.GetByOrderId(request.OrderId, ct);
        if (payment is null)
            return ApiResponse.ErrorResponse(ErrorCodes.NotFound, "Payment not found");

        if (payment.Status is PaymentStatus.Ready or PaymentStatus.Pending or PaymentStatus.Authorized or PaymentStatus.Captured)
            return ApiResponse.SuccessResponse();

        if (payment.Status is PaymentStatus.Cancelled or PaymentStatus.Refunded or PaymentStatus.Failed or PaymentStatus.Starting)
            return ApiResponse.SuccessResponse();

        payment.MarkReady();
        await uow.SaveChangesAsync(ct);

        return ApiResponse.SuccessResponse();
    }
}
