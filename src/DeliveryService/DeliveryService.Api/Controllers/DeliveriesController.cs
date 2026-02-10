using System.Text.Json;
using System.Threading.Channels;
using DeliveryService.Api.DTOs;
using DeliveryService.Application.Commands.AcceptDelivery;
using DeliveryService.Application.Commands.CancelDelivery;
using DeliveryService.Application.Commands.CompleteDelivery;
using DeliveryService.Application.Commands.DeclineDelivery;
using DeliveryService.Application.Commands.FailDelivery;
using DeliveryService.Application.Commands.MarkInTransit;
using DeliveryService.Application.Commands.MarkPickedUp;
using DeliveryService.Application.Commands.ReturnDelivery;
using DeliveryService.Application.Queries.GetDelivery;
using DeliveryService.Application.Queries.GetDeliveryByOrder;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Services;
using Shared.Utilities;
using StackExchange.Redis;

namespace DeliveryService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeliveriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILocationTrackingClient _trackingClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly bool _streamEnabled;

    public DeliveriesController(
        IMediator mediator,
        ILocationTrackingClient trackingClient,
        IConnectionMultiplexer redis,
        IConfiguration config)
    {
        _mediator = mediator;
        _trackingClient = trackingClient;
        _redis = redis;
        var mode = config["Delivery:Tracking:Mode"] ?? "stream";
        _streamEnabled = !string.Equals(mode, "poll", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDelivery(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDeliveryQuery(id), ct);
        if (!result.Success)
            return NotFound(result);

        if (result.Data != null)
            result.Data.VerificationCode = null;

        return Ok(result);
    }

    [HttpGet("by-order/{orderId:guid}")]
    public async Task<IActionResult> GetByOrder(Guid orderId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDeliveryByOrderQuery(orderId), ct);
        if (!result.Success)
            return NotFound(result);

        if (result.Data != null)
            result.Data.VerificationCode = null;

        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, [FromBody] AcceptDeliveryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AcceptDeliveryCommand(id, request.CourierId), ct);
        if (!result.Success)
            return BadRequest(result);

        var delivery = await _mediator.Send(new GetDeliveryQuery(id), ct);
        return Ok(delivery);
    }

    [Authorize]
    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, [FromBody] DeclineDeliveryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeclineDeliveryCommand(id, request.CourierId, request.Reason), ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/pickup")]
    public async Task<IActionResult> PickUp(Guid id, [FromBody] CourierActionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkPickedUpCommand(id, request.CourierId), ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/start")]
    public async Task<IActionResult> StartDelivery(Guid id, [FromBody] CourierActionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new MarkInTransitCommand(id, request.CourierId), ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteDeliveryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CompleteDeliveryCommand(
            id,
            request.CourierId,
            request.Signature,
            request.PhotoUrl,
            request.Notes,
            request.VerificationCode), ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/fail")]
    public async Task<IActionResult> Fail(Guid id, [FromBody] FailDeliveryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new FailDeliveryCommand(id, request.Reason), ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/return")]
    public async Task<IActionResult> Return(Guid id, [FromBody] ReturnDeliveryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReturnDeliveryCommand(id, request.Reason), ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelDeliveryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CancelDeliveryCommand(id, request.Reason), ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }

    [HttpGet("{id:guid}/track")]
    public async Task<IActionResult> Track(Guid id, CancellationToken ct)
    {
        var delivery = await _mediator.Send(new GetDeliveryQuery(id), ct);
        if (!delivery.Success || delivery.Data == null)
            return NotFound(delivery);

        if (!delivery.Data.CourierId.HasValue)
            return Conflict(ApiResponse.ErrorResponse("Courier is not assigned"));

        var location = await _trackingClient.GetCourierLocationAsync(delivery.Data.CourierId.Value);

        return Ok(new
        {
            deliveryId = id,
            courierId = delivery.Data.CourierId,
            status = delivery.Data.Status,
            latitude = location.Latitude,
            longitude = location.Longitude,
            isOnline = location.IsOnline
        });
    }

    [HttpGet("{id:guid}/track/history")]
    public async Task<IActionResult> TrackHistory(Guid id, [FromQuery] long fromTimestampMs = 0, [FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var delivery = await _mediator.Send(new GetDeliveryQuery(id), ct);
        if (!delivery.Success || delivery.Data == null)
            return NotFound(delivery);

        if (!delivery.Data.CourierId.HasValue)
            return Conflict(ApiResponse.ErrorResponse("Courier is not assigned"));

        var history = await _trackingClient.GetCourierLocationHistoryAsync(
            delivery.Data.CourierId.Value,
            fromTimestampMs,
            limit);

        return Ok(new
        {
            deliveryId = id,
            courierId = delivery.Data.CourierId,
            points = history.Select(h => new
            {
                latitude = h.Latitude,
                longitude = h.Longitude,
                timestampMs = h.TimestampMs,
                accuracy = h.Accuracy
            })
        });
    }

    [HttpGet("{id:guid}/track/stream")]
    public async Task TrackStream(Guid id, CancellationToken ct)
    {
        if (!_streamEnabled)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var delivery = await _mediator.Send(new GetDeliveryQuery(id), ct);
        if (!delivery.Success || delivery.Data == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        if (!delivery.Data.CourierId.HasValue)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }

        var courierId = delivery.Data.CourierId.Value;
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Content-Type"] = "text/event-stream";

        var channel = Channel.CreateUnbounded<string>();
        var subscriber = _redis.GetSubscriber();

        var subscription = await subscriber.SubscribeAsync("courier.location.updated");
        subscription.OnMessage(message =>
        {
            if (message.Message.IsNullOrEmpty)
                return;

            try
            {// TODO check message
                using var doc = JsonDocument.Parse(message.Message.ToString());
                if (!doc.RootElement.TryGetProperty("CourierId", out var idValue))
                    return;

                if (Guid.TryParse(idValue.GetString(), out var parsed) && parsed == courierId)
                {
                    channel.Writer.TryWrite(message.Message!);
                }
            }
            catch
            {
                // ignore malformed
            }
        });

        await foreach (var data in channel.Reader.ReadAllAsync(ct))
        {
            await Response.WriteAsync($"data: {data}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }
    }
}
