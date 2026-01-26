using CourierService.Application.Commands.RegisterCourier;
using CourierService.Application.Commands.UpdateCourierStatus;
using CourierService.Application.Models;
using CourierService.Application.Queries.GetActiveCouriers;
using CourierService.Application.Queries.GetCourier;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CourierService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouriersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CouriersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourier(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCourierQuery(id), ct);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourier([FromBody] CreateCourierModel model, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _mediator.Send(new RegisterCourierCommand(model), ct);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetCourier), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourier(Guid id, [FromBody] UpdateCourierModel model, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCourierStatusCommand(id, model), ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCouriers(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetActiveCouriersQuery(), ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "CourierService" });
    }
}
