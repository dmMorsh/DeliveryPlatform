using MediatR;
using Microsoft.AspNetCore.Mvc;
using InventoryService.Application.Commands.ReserveStock;

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
    public async Task<IActionResult> Reserve(Guid productId, [FromBody] ReserveStockRequest request)
    {
        var cmd = new ReserveStockCommand(productId, request.OrderId, request.Quantity);
        var result = await _mediator.Send(cmd);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
}

public record ReserveStockRequest(Guid OrderId, int Quantity);
