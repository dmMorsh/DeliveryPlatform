using Microsoft.AspNetCore.Mvc;
using MediatR;
using Shared.Contracts;
using CourierService.Application.Commands.RegisterCourier;
using CourierService.Application.Commands.UpdateCourierStatus;
using CourierService.Application.Queries.GetCourier;
using CourierService.Application.Queries.GetActiveCouriers;

namespace CourierService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouriersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CouriersController> _logger;

    public CouriersController(IMediator mediator, ILogger<CouriersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourier(Guid id)
    {
        var result = await _mediator.Send(new GetCourierQuery(id));
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourier([FromBody] CreateCourierDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _mediator.Send(new RegisterCourierCommand(dto));
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetCourier), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourier(Guid id, [FromBody] UpdateCourierDto dto)
    {
        var result = await _mediator.Send(new UpdateCourierStatusCommand(id, dto));
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCouriers()
    {
        var result = await _mediator.Send(new GetActiveCouriersQuery());
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
