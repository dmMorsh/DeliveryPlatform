using GatewayApi.DTOs;
using GatewayApi.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace GatewayApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly IProxyService _proxyService;


    public AuthController(ILogger<AuthController> logger, IProxyService proxyService)
    {
        _logger = logger;
        _proxyService = proxyService;
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Gateway: Login {e}", request.Email);

        var (data, statusCode, error) = await _proxyService.ProxyPostAsync<dynamic>(
            "auth-service",
            "/api/auth/login",
            HttpContext,
            request,
            ct
        );

        if (statusCode >= 200 && statusCode < 300)
        {
            return StatusCode(statusCode, data);
        }

        _logger.LogError("Error login: {Error}", error);
        return StatusCode(statusCode, new ProxyErrorResponse { Message = error });
    }
}