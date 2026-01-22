using MediatR;
using Microsoft.AspNetCore.Mvc;
using ServiceName.Application.Commands.CreateServiceName;
using ServiceName.Application.Models;

namespace ServiceName.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceNameController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServiceNameController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceNameModel model)
    {
        var cmd = new CreateServiceNameCommand(model);
        var result = await _mediator.Send(cmd);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(Guid id) => NotFound();
}