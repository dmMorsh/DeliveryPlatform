using Mapster;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderService.Api.Contracts;
using OrderService.Application.Commands.CancelOrder;
using OrderService.Application.Commands.CreateOrder;
using OrderService.Application.Commands.MarkOrderReady;
using OrderService.Application.Commands.MarkOrderAccepted;
using OrderService.Application.Commands.MarkOrderRejected;
using OrderService.Application.Commands.UpdateOrder;

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
    /// Create a new order
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
        
        return Ok(result);
    }

    /// <summary>
    /// Update an order
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
    /// Cancel an order
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest request, CancellationToken ct)
    {
        var cmd = new CancelOrderCommand(id, request?.Reason);
        var result = await _mediator.Send(cmd, ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Mark order as ready (for example, kitchen/supplier)
    /// </summary>
    [HttpPost("{id}/ready")]
    public async Task<IActionResult> MarkReady(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkOrderReadyCommand(id), ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Accept order (kitchen/supplier)
    /// </summary>
    [HttpPost("{id}/accept")]
    public async Task<IActionResult> MarkAccepted(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkOrderAcceptedCommand(id), ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Reject order (kitchen/supplier)
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<IActionResult> MarkRejected(Guid id, [FromBody] CancelOrderRequest? request, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkOrderRejectedCommand(id, request?.Reason), ct);
        if (!result.Success)
            return BadRequest(result);
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
