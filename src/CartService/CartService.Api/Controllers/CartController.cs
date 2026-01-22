using CartService.Application.Commands.AddItem;
using CartService.Application.Commands.Checkout;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace CartService.Api.Controllers;

[ApiController]
[Route("cart")]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddItemRequest request)
    {
        var customerId = Guid.NewGuid(); // TODO: Get from Identity/JWT

        var cmd = new AddItemToCartCommand(
            customerId,
            request.ProductId,
            request.Name,
            request.Price,
            request.Quantity
        );

        var result = await _mediator.Send(cmd);
        
        if (!result.Success)
            return BadRequest(result);

        return Ok(result);
    }
    
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout()
    {
        var customerId = Guid.NewGuid(); // TODO: Get from Identity/JWT

        var cmd = new CheckoutCartCommand(customerId);
        var result = await _mediator.Send(cmd);

        if (!result.Success)
            return BadRequest(result);

        return Ok(new { cartId = result.Data });
    }
}

public record AddItemRequest(Guid ProductId, string Name, int Price, int Quantity);
