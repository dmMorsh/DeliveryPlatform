using Microsoft.AspNetCore.Mvc;

namespace ServiceName.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ServiceNameController : ControllerBase
{
    private readonly ILogger<ServiceNameController> _logger;

    public ServiceNameController(ILogger<ServiceNameController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> Get()
    {
        return Task.FromResult<IActionResult>(Ok("result"));
    }
}