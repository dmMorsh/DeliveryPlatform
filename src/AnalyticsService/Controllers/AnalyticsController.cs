using Microsoft.AspNetCore.Mvc;
using AnalyticsService.Services;

namespace AnalyticsService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly AnalyticsEventConsumer _consumer;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(AnalyticsEventConsumer consumer, ILogger<AnalyticsController> logger)
    {
        _consumer = consumer;
        _logger = logger;
    }

    /// <summary>
    /// Получить текущую статистику
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var snapshot = _consumer.GetSnapshot();
        return Ok(snapshot);
    }

    /// <summary>
    /// Prometheus metrics
    /// </summary>
    [HttpGet("metrics")]
    public IActionResult GetMetrics()
    {
        var snapshot = _consumer.GetSnapshot();
        var metrics = new List<string>
        {
            $"# HELP delivery_orders_total Total number of orders",
            $"# TYPE delivery_orders_total gauge",
            $"delivery_orders_total {snapshot.TotalOrders}",
            $"",
            $"# HELP delivery_orders_delivered Total delivered orders",
            $"# TYPE delivery_orders_delivered gauge",
            $"delivery_orders_delivered {snapshot.DeliveredOrders}",
            $"",
            $"# HELP delivery_couriers_total Total number of couriers",
            $"# TYPE delivery_couriers_total gauge",
            $"delivery_couriers_total {snapshot.TotalCouriers}",
        };

        return Ok(string.Join("\n", metrics));
    }
}

/// <summary>
/// Health check endpoint
/// </summary>
[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", service = "AnalyticsService" });
    }
}
