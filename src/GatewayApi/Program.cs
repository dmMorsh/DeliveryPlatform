using System.Diagnostics;
using System.Text;
using System.Threading.RateLimiting;
using GatewayApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry;
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
    new LocationTrackingClientImpl(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<IHostEnvironment>(),
        sp.GetRequiredService<ILogger<LocationTrackingClientImpl>>()));

// Auth
var jwtKey = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Key");
var jwtIssuer = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Issuer");
var jwtAudience = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Audience");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Headers["authorization"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    context.Token = accessToken.ToString().Replace("Bearer ", "");
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// CORS для всех источников (Development mode)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            return;
        }

        var gatewayCorsOrigins = ConfigurationGuard.GetRequiredArray(builder.Configuration, builder.Environment, "Cors:AllowedOrigins");
        policy.WithOrigins(gatewayCorsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

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
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out var headerValue)
        ? headerValue.ToString()
        : null;

    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = context.TraceIdentifier;
        context.Request.Headers["X-Correlation-Id"] = correlationId;
    }

    if (Activity.Current != null)
    {
        Activity.Current.SetTag("correlation.id", correlationId);
    }

    Baggage.SetBaggage("correlation.id", correlationId);

    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        return Task.CompletedTask;
    });

    await next();
});
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
