namespace PaymentService.Api.Security;

public interface IWebhookValidator
{
    bool IsValid(HttpContext context);
}
