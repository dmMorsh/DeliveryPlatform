using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderReadService.Application.Interfaces;
using OrderReadService.Application.Queries.GetClientOrders;
using OrderReadService.Application.Queries.GetOrder;
using OrderReadService.Infrastructure.Persistence;

namespace OrderReadService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly OrderReadDbContext _db;

    private readonly IOrderReadCache _cache;
    public OrdersController(OrderReadDbContext db, IOrderReadCache cache, IMediator mediator)
    {
        _db = db;
        _cache = cache;
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var cached = await _cache.GetAsync(id, HttpContext.RequestAborted);
        if (cached != null) return Ok(cached);

        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id);
        if (order == null) return NotFound();
        await _cache.SetAsync(order, HttpContext.RequestAborted);
        return Ok(order);
    }
    
    
    /// <summary>
    /// Получить заказ по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id, CancellationToken ct)
    {
        var customerId = GetCustomerIdFromContext();
        if (customerId == Guid.Empty)
            return Unauthorized(new { error = "Customer ID not found in context" });
        
        var query = new GetOrderQuery(id, customerId);
        
        var result = await _mediator.Send(query, ct);
        if (!result.Success)
            return NotFound(result);
        
        return Ok(result);
    }
    
    /// <summary>
    /// Получить заказы клиента
    /// </summary>
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
