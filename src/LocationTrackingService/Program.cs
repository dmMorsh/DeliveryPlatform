using Serilog;
using LocationTrackingService.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .WriteTo.File("logs/locationtrackingservice-YYYY-MM-DD.log", 
           rollingInterval: RollingInterval.Day, 
           outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .MinimumLevel.Information());

// Add services
builder.Services.AddGrpc();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<ILocationService, LocationService>();

// Redis configuration
var redisConn = builder.Configuration["Redis:Connection"] ?? "localhost:6379";
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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseCors();
app.MapHealthChecks("/health");

// Map gRPC services
app.MapGrpcService<LocationTrackingServiceImpl>();

// REST API for health check
app.MapGet("/health", () => 
    Results.Ok(new { status = "healthy", service = "LocationTrackingService" }));

Log.Information("LocationTrackingService started. gRPC on {Address}", app.Urls.FirstOrDefault() ?? "https://0.0.0.0:7070");

await app.RunAsync();

app.Run();
