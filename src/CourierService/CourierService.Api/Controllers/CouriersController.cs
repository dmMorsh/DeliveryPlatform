using Microsoft.AspNetCore.Mvc;
using Shared.Contracts;
using CourierService.Services;

namespace CourierService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CouriersController : ControllerBase
{
    private readonly CourierApplicationService _courierService;
    private readonly ILogger<CouriersController> _logger;

    public CouriersController(CourierApplicationService courierService, ILogger<CouriersController> logger)
    {
        _courierService = courierService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourier(Guid id)
    {
        var result = await _courierService.GetCourierAsync(id);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourier([FromBody] CreateCourierDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _courierService.CreateCourierAsync(dto);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetCourier), new { id = result.Data?.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCourier(Guid id, [FromBody] UpdateCourierDto dto)
    {
        var result = await _courierService.UpdateCourierAsync(id, dto);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveCouriers()
    {
        var result = await _courierService.GetActiveCouriersAsync();
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
