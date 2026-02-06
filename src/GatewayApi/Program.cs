using GatewayApi.Services;
using Serilog;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .WriteTo.File("../../logs/gateway-.log", 
           rollingInterval: RollingInterval.Day, 
           outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .MinimumLevel.Information());

// Регистрация сервисов
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IProxyService, ProxyService>();
// gRPC Location Tracking Client for GatewayApi
builder.Services.AddScoped<ILocationTrackingClient>(sp => 
    new LocationTrackingClientImpl(sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<ILogger<LocationTrackingClientImpl>>()));

// CORS для всех источников (Development mode)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();
app.MapHealthChecks("/health");

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Gateway API started. Service URLs - OrderService: {OrderService}, CourierService: {CourierService}",
    builder.Configuration["Services:OrderServiceUrl"] ?? "http://localhost:5204",
    builder.Configuration["Services:CourierServiceUrl"] ?? "http://localhost:5205");

app.Run();
