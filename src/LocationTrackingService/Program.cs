using Serilog;
using LocationTrackingService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.WriteTo.Console()
       .MinimumLevel.Information());

// Add services
builder.Services.AddGrpc();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<ICourierLocationService, CourierLocationService>();

// Redis configuration (used for location store)
var redisConn = builder.Configuration["Redis:Connection"] ?? "localhost:6379";
var mux = StackExchange.Redis.ConnectionMultiplexer.Connect(redisConn);
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(mux);
builder.Services.AddSingleton(sp => sp.GetRequiredService<StackExchange.Redis.IConnectionMultiplexer>().GetDatabase());

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
app.MapGrpcService<LocationTrackingGrpcService>();

// REST API for health check
app.MapGet("/health", () => 
    Results.Ok(new { status = "healthy", service = "LocationTrackingService" }));

Log.Information("LocationTrackingService started. gRPC on {Address}", app.Urls.FirstOrDefault() ?? "http://0.0.0.0:5003");

app.Run();
