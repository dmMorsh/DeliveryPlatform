using CatalogService.Api.Contracts;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using CatalogService.Application.Commands.CreateProduct;
using CatalogService.Application.Commands.UpdateProduct;
using CatalogService.Application.Models;
using CatalogService.Application.Queries.GetProductById;
using CatalogService.Application.Queries.SearchProducts;

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
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductModel model, CancellationToken ct)
    {
        var cmd = new CreateProductCommand(model);
        var result = await _mediator.Send(cmd, ct);
        
        if (!result.Success)
            return BadRequest(result);
        
        // return Ok(result);
        return CreatedAtAction(nameof(GetProductById), new { id = result.Data?.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct)
    {
        var query = new GetProductByIdQuery(id);
        
        var result = await _mediator.Send(query, ct);

        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
    
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] SearchProductsRequest request,
        CancellationToken ct)
    {
        var query = new SearchProductsQuery(
            request.Text,
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            request.SortBy,
            request.SortDir,
            request.Page,
            request.PageSize
        );

        var result = await _mediator.Send(query, ct);

        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductModel model, CancellationToken ct)
    {
        var cmd = new UpdateProductCommand(id, model);
        var result = await _mediator.Send(cmd, ct);
        
        if (!result.Success)
            return BadRequest(result);
            
        return Ok(result);
    }
}