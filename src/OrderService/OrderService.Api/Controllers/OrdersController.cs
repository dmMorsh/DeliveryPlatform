using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Contracts;
using OrderService.Application.Commands.CreateOrder;
using OrderService.Application.Commands.UpdateOrder;
using OrderService.Application.Queries.GetClientOrders;
using OrderService.Application.Queries.GetOrder;

namespace OrderService.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Получить заказ по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var query = new GetOrderQuery(id);
        
        var result = await _mediator.Send(query, ct);
        if (!result.Success)
            return NotFound(result);
        
        return Ok(result);
    }

    /// <summary>
    /// Создать новый заказ
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest createRequest, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var cmd = createRequest.Adapt<CreateOrderCommand>();

        var result = await _mediator.Send(cmd, ct);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetOrder), new { id = result.Data?.Id }, result);
    }

    /// <summary>
    /// Обновить заказ
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] UpdateOrderRequest updateOrderRequest, CancellationToken ct)
    {
        var cmd = new UpdateOrderCommand(
            id,
            updateOrderRequest.CourierId,
            updateOrderRequest.CourierName,
            updateOrderRequest.Status,
            updateOrderRequest.CourierNote
        );
        var result = await _mediator.Send(cmd, ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Получить заказы клиента
    /// </summary>
    // [HttpGet("client/{clientId}")]
    [HttpGet]
    public async Task<IActionResult> GetClientOrders(CancellationToken ct)
    {
        var customerId = GetCustomerIdFromContext();
        if (customerId == Guid.Empty)
            return Unauthorized(new { error = "Customer ID not found in context" });
        
        var query = new GetClientOrdersQuery(customerId);
        
        var result = await _mediator.Send(query, ct);
        if (!result.Success)
            return NotFound(result);
        
        return Ok(result);
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
