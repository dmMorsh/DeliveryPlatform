using Hangfire;
using OrderService.Api.CompositionRoot;
using OrderService.Api.Grpc;
using OrderService.Api.Mappings;
using Shared.Middleware;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Service identity + logging
builder.AddServiceTelemetry("order-service");
builder.UseExtededSerilog();

// API surface
builder.Services.AddOrderServiceApi();

// Runtime flags
var settings = OrderServiceSettings.From(builder.Configuration, builder.Environment);

if (settings.UseInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

// Composition root
builder.Services.AddOrderServiceCore(builder.Configuration, settings);

// Auth + CORS
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

// Health + throttling
builder.Services.AddOrderServiceHealthChecks(
    settings);
builder.Services.AddAdaptiveThrottle(settings);

var app = builder.Build();

MapsterConfig.RegisterMappings();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Middleware
app.UseRouting();
app.UseDistributedRateLimit();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapGrpcService<OrderGrpcService>();
app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();
app.MapOrderServiceHealthChecks();

// Startup tasks
app.RunOrderServiceMigrationsAndJobs(settings.UseInMemory, builder.Configuration);

app.UseHangfireDashboard();

app.Run();
