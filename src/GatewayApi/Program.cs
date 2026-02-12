using System.Threading.RateLimiting;
using GatewayApi.Services;
using Serilog;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    var requiredServiceUrls = new[]
    {
        "Services:AuthServiceUrl",
        "Services:CatalogServiceUrl",
        "Services:CartServiceUrl",
        "Services:InventoryServiceUrl",
        "Services:OrderServiceUrl",
        "Services:CourierServiceUrl",
        "Services:LocationTrackingUrl"
    };

    foreach (var key in requiredServiceUrls)
        ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, key);

    ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "gRPC:LocationTrackingService:Url");
}

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .WriteTo.File("../../logs/gateway-.log", 
           rollingInterval: RollingInterval.Day, 
           outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .MinimumLevel.Information());

// Регистрация сервисов
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient("proxy")
    .AddPolicyHandler((sp, _) =>
        HttpResiliencePolicies.CreatePolicyWrap(sp.GetRequiredService<ILoggerFactory>().CreateLogger("GatewayHttpClient")));
builder.Services.AddScoped<IProxyService, ProxyService>();
builder.AddServiceTelemetry("gateway-api");
// gRPC Location Tracking Client for GatewayApi
builder.Services.AddScoped<ILocationTrackingClient>(sp => 
    new LocationTrackingClient(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHostEnvironment>(),
        sp.GetRequiredService<ILogger<LocationTrackingClient>>()));

// Auth
builder.AddExtededAuthentication();
builder.Services.AddAuthorization();
builder.AddExtededCors();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("gateway-default", httpContext =>
    {
        var key = httpContext.User?.Identity?.IsAuthenticated == true
            ? $"user:{httpContext.User.Identity!.Name ?? httpContext.User.FindFirst("sub")?.Value ?? "unknown"}"
            : $"ip:{httpContext.Connection.RemoteIpAddress}";

        return RateLimitPartition.GetTokenBucketLimiter(key, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 120,
            TokensPerPeriod = 120,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCorrelationId();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers().RequireRateLimiting("gateway-default");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true
});

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Gateway API started. Service URLs - OrderService: {OrderService}, CourierService: {CourierService}",
    builder.Configuration["Services:OrderServiceUrl"] ?? "http://localhost:5204",
    builder.Configuration["Services:CourierServiceUrl"] ?? "http://localhost:5205");

app.Run();
