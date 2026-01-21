using Microsoft.AspNetCore.Mvc;

namespace InventoryService.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class InventoryServiceController : ControllerBase
{
    private readonly ILogger<InventoryServiceController> _logger;

    public InventoryServiceController(ILogger<InventoryServiceController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public Task<IActionResult> Get()
    {
        return Task.FromResult<IActionResult>(Ok("result"));
    }
}