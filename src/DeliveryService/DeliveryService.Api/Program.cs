using DeliveryService.Api.CompositionRoot;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Service identity + logging
builder.AddServiceTelemetry("delivery-service")
    .WithMetrics(m => m.AddMeter("DeliveryService.Assignment"));
builder.UseExtededSerilog();

// API surface
builder.Services.AddDeliveryServiceApi();

// Runtime flags
var settings = DeliveryServiceSettings.From(builder.Configuration, builder.Environment);

if (settings.UseInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

// Composition root
builder.Services.AddDeliveryServiceCore(builder.Configuration, settings);

// Auth + CORS
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

// Health checks
builder.Services.AddDeliveryServiceHealthChecks(settings);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Middleware
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapControllers();
app.MapDeliveryServiceHealthChecks();

// Startup tasks
app.RunDeliveryServiceMigrationsAndJobs(settings.UseInMemory, builder.Configuration);

app.Run();
