using CatalogReadService.Api.Contracts;
using CatalogReadService.Application.Queries.GetProductById;
using CatalogReadService.Application.Queries.SearchProducts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CatalogReadService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CatalogController : ControllerBase
{
    private readonly IMediator _mediator;

    public CatalogController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _mediator.Send(query, ct);

        if (!result.Success)
            return NotFound(result);

        return Ok(result);
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] SearchProductsRequest request, CancellationToken ct)
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
}
