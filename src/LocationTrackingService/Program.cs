using LocationTrackingService.Services;
using Serilog;
using StackExchange.Redis;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .WriteTo.File("../../logs/LocationTrackingService-.log", 
           rollingInterval: RollingInterval.Day, 
           outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .MinimumLevel.Information());

// Add services
builder.Services.AddGrpc();
builder.Services.AddOpenApi();
builder.AddServiceTelemetry("location-tracking-service");
builder.Services.AddScoped<ILocationService, LocationService>();

// Redis configuration
var redisConn = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Redis:Connection", "localhost:6379");
try
{
    var mux = ConnectionMultiplexer.Connect(redisConn);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
    builder.Services.AddSingleton(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
}
catch (Exception ex)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => throw new InvalidOperationException($"Failed to connect to Redis at {redisConn}", ex));
}

builder.Services.AddHealthChecks()
    .AddRedis(redisConn, name: "redis", tags: new[] { "ready" })
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "ready" });

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

        var locationCorsOrigins = ConfigurationGuard.GetRequiredArray(builder.Configuration, builder.Environment, "Cors:AllowedOrigins");
        policy.WithOrigins(locationCorsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = reg => reg.Tags.Contains("ready")
});

// Map gRPC services
app.MapGrpcService<LocationTrackingServiceImpl>();

Log.Information("LocationTrackingService started. gRPC on {Address}", app.Urls.FirstOrDefault() ?? "https://0.0.0.0:7070");

await app.RunAsync();

app.Run();
