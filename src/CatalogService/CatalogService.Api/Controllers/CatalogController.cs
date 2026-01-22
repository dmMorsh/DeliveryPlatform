using MediatR;
using Microsoft.AspNetCore.Mvc;
using CatalogService.Application.Commands.CreateProduct;
using CatalogService.Application.Commands.UpdateProduct;
using CatalogService.Application.Models;

namespace CatalogService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly IMediator _mediator;

    public CatalogController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductModel model)
    {
        var cmd = new CreateProductCommand(model);
        var result = await _mediator.Send(cmd);
        
        if (!result.Success)
            return BadRequest(result);
            
        return CreatedAtAction(nameof(GetProduct), new { id = result.Data?.Id }, result);
    }

    [HttpGet("{id}")]
    public IActionResult GetProduct(Guid id)
    {
        // TODO: Implement query handler for retrieving product
        return NotFound();
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request)
    {
        var cmd = new UpdateProductCommand(id, request.Name, request.Description, request.PriceCents, request.StockQuantity);
        var result = await _mediator.Send(cmd);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
}

public record UpdateProductRequest(string? Name, string? Description, long? PriceCents, int? StockQuantity);