namespace PaymentService.Application.Interfaces;

public interface IPaymentStatusCheckScheduler
{
    void ScheduleStatusCheck(Guid orderId);
}
