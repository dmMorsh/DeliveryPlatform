using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Api.Contracts;
using PaymentService.Application.Commands.CancelPayment;
using PaymentService.Application.Commands.CapturePayment;
using PaymentService.Application.Commands.CreatePayment;
using PaymentService.Application.Commands.RefundPayment;
using PaymentService.Application.Commands.StartPayment;
using PaymentService.Application.Interfaces;
using PaymentService.Application.Models;
using Shared.Utilities;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWorkFactory _factory;
    private readonly IConfiguration _config;
    private readonly IHostEnvironment _env;

    public PaymentController(IMediator mediator, IUnitOfWorkFactory factory, IConfiguration config, IHostEnvironment env)
    {
        _mediator = mediator;
        _factory = factory;
        _config = config;
        _env = env;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentRequest request)
    {
        var allowManual = bool.TryParse(_config["Payments:AllowManualCreate"], out var allowed) && allowed;
        if (!allowManual)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse.ErrorResponse(ErrorCodes.Validation, "Manual payment creation is disabled"));

        var cmd = new CreatePaymentCommand(request.OrderId, request.Amount, request.Currency);
        var result = await _mediator.Send(cmd);
        if (!result.Success)
            return BadRequest(result);
        return CreatedAtAction(nameof(GetById), new { id = result.Message }, result);
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartPaymentModel model)
    {
        var cmd = new StartPaymentCommand(model.OrderId, model.Provider, model.Capture);
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpPost("capture")]
    public async Task<IActionResult> Capture([FromBody] CapturePaymentModel model)
    {
        var cmd = new CapturePaymentCommand(model.OrderId, model.AmountCents);
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel([FromBody] CancelPaymentModel model)
    {
        var cmd = new CancelPaymentCommand(model.OrderId);
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpPost("refund")]
    public async Task<IActionResult> Refund([FromBody] RefundPaymentModel model)
    {
        var cmd = new RefundPaymentCommand(model.OrderId, model.AmountCents);
        var result = await _mediator.Send(cmd);
        return Ok(result);
    }

    [HttpGet("status/{orderId:guid}")]
    public async Task<IActionResult> GetStatus(Guid orderId, CancellationToken ct)
    {
        await using var uow = _factory.Create(orderId);
        var payment = await uow.Payments.GetByOrderId(orderId, ct);
        if (payment is null)
            return NotFound(ApiResponse<PaymentStatusView>.ErrorResponse(ErrorCodes.NotFound, "Payment not found"));

        var view = new PaymentStatusView
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Status = payment.Status.ToString(),
            Provider = payment.Provider ?? string.Empty,
            ExternalPaymentId = string.IsNullOrWhiteSpace(payment.ExternalPaymentId) ? null : payment.ExternalPaymentId,
            PaymentUrl = string.IsNullOrWhiteSpace(payment.PaymentUrl) ? null : payment.PaymentUrl,
            AmountCents = payment.AmountCents,
            Currency = payment.Currency
        };

        return Ok(ApiResponse<PaymentStatusView>.SuccessResponse(view));
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id) => NotFound();
}
