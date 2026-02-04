using MediatR;
using Microsoft.AspNetCore.Mvc;
using InventoryService.Application.Commands.ReserveStock;
using InventoryService.Application.Models;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IMediator _mediator;

    public StockController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{orderId}/reserve")]
    public async Task<IActionResult> Reserve(Guid orderId, [FromBody] ReserveStockModel[] model, CancellationToken ct)
    {
        var cmd = new ReserveStockCommand(orderId, model);
        var result = await _mediator.Send(cmd, ct);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
}