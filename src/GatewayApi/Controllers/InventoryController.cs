using GatewayApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly ILogger<InventoryController> _logger;
    private readonly IProxyService _proxyService;

    public InventoryController(ILogger<InventoryController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }
}