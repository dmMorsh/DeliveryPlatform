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

    [HttpPost("{productId}/reserve")]
    public async Task<IActionResult> Reserve(Guid productId, [FromBody] ReserveStockModel model, CancellationToken ct)
    {
        var cmd = new ReserveStockCommand(productId, model.OrderId, model.Quantity);
        var result = await _mediator.Send(cmd, ct);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
}