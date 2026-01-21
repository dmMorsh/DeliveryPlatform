using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Application;
using OrderService.Application.Commands.CreateOrder;
using OrderService.Application.Commands.UpdateOrder;
using OrderService.Application.Queries.GetOrder;

namespace OrderService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    
    private readonly ILogger<OrdersController> _logger;//TODO удалить?

    public OrdersController(ILogger<OrdersController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Получить заказ по ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var cmd = new GetOrderQuery(id);
        
        var result = await _mediator.Send(cmd);
        if (!result.Success)
            return NotFound(result);
        
        return Ok(result);
    }

    /// <summary>
    /// Создать новый заказ
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderModel createModel)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        
        var cmd = new CreateOrderCommand(createModel);

        var result = await _mediator.Send(cmd);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetOrder), new { id = result.Data?.Id }, result);
    }

    /// <summary>
    /// Обновить заказ
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrder(Guid id, [FromBody] UpdateOrderModel updateOrderModel)
    {
        var cmd = new UpdateOrderCommand(id, updateOrderModel);
        var result = await _mediator.Send(cmd);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// Получить заказы клиента
    /// </summary>
    // [HttpGet("client/{clientId}")]
    // public async Task<IActionResult> GetClientOrders(Guid clientId)
    // {
    //     var result = await _orderService.GetClientOrdersAsync(clientId);
    //     if (!result.Success)
    //         return BadRequest(result);
    //     return Ok(result);
    // }

    /// <summary>
    /// Health check
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "OrderService" });
    }
}
