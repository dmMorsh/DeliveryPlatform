using MediatR;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Commands.CreatePayment;
using PaymentService.Application.Models;

namespace PaymentService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;

    public PaymentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePaymentModel model)
    {
        var cmd = new CreatePaymentCommand(model);
        var result = await _mediator.Send(cmd);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id) => NotFound();
}