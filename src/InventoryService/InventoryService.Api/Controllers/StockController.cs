using InventoryService.Application.Commands.AddStock;
using InventoryService.Application.Commands.AdjustStock;
using InventoryService.Application.Commands.ReleaseStock;
using InventoryService.Application.Commands.ReserveStock;
using InventoryService.Application.Models;
using InventoryService.Application.Queries.GetStocks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> Reserve(Guid orderId, [FromBody] SimpleStockItemModel[] models, CancellationToken ct)
    {
        var cmd = new ReserveStockCommand(orderId, models);
        var result = await _mediator.Send(cmd, ct);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPost("{orderId}/release")]
    public async Task<IActionResult> Release(Guid orderId, [FromBody] SimpleStockItemModel[] models, CancellationToken ct)
    {
        var cmd = new ReleaseStockCommand(orderId, models);
        var result = await _mediator.Send(cmd, ct);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetStocks(CancellationToken ct)
    {
        var cmd = new GetStocksQuery();
        var result = await _mediator.Send(cmd, ct);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpPost]
    public async Task<IActionResult> AddStocks(SimpleStockItemModel[] models, CancellationToken ct)
    {
        var cmd = new AddStockCommand(models);
        var result = await _mediator.Send(cmd, ct);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpPut]
    public async Task<IActionResult> AdjustStocks(SimpleStockItemModel[] models, CancellationToken ct)
    {
        var cmd = new AdjustStockCommand(models);
        var result = await _mediator.Send(cmd, ct);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
}