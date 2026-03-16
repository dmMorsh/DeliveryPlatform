using CourierService.Api.Contracts;
using CourierService.Application.Commands.RegisterCourier;
using CourierService.Application.Commands.UpdateCourierStatus;
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
    public async Task<IActionResult> CreateCourier([FromBody] CreateCourierRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _mediator.Send(new RegisterCourierCommand(
            request.FullName,
            request.Phone,
            request.Email,
            request.DocumentNumber
        ), ct);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetCourier), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourier(Guid id, [FromBody] UpdateCourierRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateCourierStatusCommand(
            id,
            request.Status,
            request.CurrentLatitude,
            request.CurrentLongitude,
            request.IsActive
        ), ct);
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
}
