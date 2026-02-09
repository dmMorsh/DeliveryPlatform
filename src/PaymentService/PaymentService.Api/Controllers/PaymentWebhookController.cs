using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PaymentService.Api.Security;
using PaymentService.Application.Commands.ProcessSberbankWebhook;
using PaymentService.Application.Commands.ProcessYooMoneyWebhook;
using PaymentService.Application.Models;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/payment/webhooks")]
[AllowAnonymous]
public class PaymentWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IWebhookValidator _validator;

    public PaymentWebhookController(IMediator mediator, IWebhookValidator validator)
    {
        _mediator = mediator;
        _validator = validator;
    }

    [HttpPost("yoomoney")]
    public async Task<IActionResult> YooMoney([FromBody] YooMoneyWebhookModel model)
    {
        if (!_validator.IsValid(HttpContext))
            return Unauthorized();

        var result = await _mediator.Send(new ProcessYooMoneyWebhookCommand(model));
        return Ok(result);
    }

    [HttpPost("sberbank")]
    public async Task<IActionResult> Sberbank([FromBody] SberbankWebhookModel model)
    {
        if (!_validator.IsValid(HttpContext))
            return Unauthorized();

        var result = await _mediator.Send(new ProcessSberbankWebhookCommand(model));
        return Ok(result);
    }
}
