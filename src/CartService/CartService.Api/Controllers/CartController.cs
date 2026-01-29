using CartService.Application.Commands.AddItem;
using CartService.Application.Commands.Checkout;
using CartService.Application.Models;
using CartService.Application.Queries.GetCart;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;

namespace CartService.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken ct)
    {
        var customerId = GetCustomerIdFromContext();
        if (customerId == Guid.Empty)
            return Unauthorized(new { error = "Customer ID not found in context" });

        var query = new GetCartQuery(customerId);

        var result = await _mediator.Send(query, ct);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddItemModel model, CancellationToken ct)
    {
        var customerId = GetCustomerIdFromContext();
        if (customerId == Guid.Empty)
            return Unauthorized(new { error = "Customer ID not found in context" });

        var cmd = new AddItemToCartCommand(customerId, model);

        var result = await _mediator.Send(cmd, ct);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutCartModel model, CancellationToken ct)
    {
        var customerId = GetCustomerIdFromContext();
        if (customerId == Guid.Empty)
            return Unauthorized(new { error = "Customer ID not found in context" });

        var cmd = new CheckoutCartCommand(customerId, model);
        var result = await _mediator.Send(cmd, ct);

        if (!result.Success)
            return BadRequest(result);

        return Ok(new { orderId = result.Data });
    }

    private Guid GetCustomerIdFromContext()
    {
        // Try to get from JWT claims first
        var userIdClaim = User?.FindFirst("sub") ?? User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var customerId))
            return customerId;

        // Fallback to User.Identity.Name if available (though should use claims)
        if (!string.IsNullOrEmpty(User?.Identity?.Name) && Guid.TryParse(User.Identity.Name, out var nameGuid))
            return nameGuid;

        // Return empty GUID if not found - will be handled by caller
        return Guid.Empty;
    }
}