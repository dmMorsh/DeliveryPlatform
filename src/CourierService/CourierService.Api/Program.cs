using CourierService.Api.CompositionRoot;
using Hangfire;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Service identity + logging
builder.AddServiceTelemetry("courier-service");
builder.UseExtededSerilog();

// API surface
builder.Services.AddCourierServiceApi();

// Runtime flags
var settings = CourierServiceSettings.From(builder.Configuration, builder.Environment);

if (settings.UseInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

// Composition root
builder.Services.AddCourierServiceCore(builder.Environment, settings);

// Auth + CORS
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

// Health checks
builder.Services.AddCourierServiceHealthChecks(
    settings);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHangfireDashboard("/hangfire");
}

// Middleware
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapControllers();
app.MapCourierServiceHealthChecks();

// Startup tasks
app.RunCourierServiceMigrations(settings.UseInMemory);

app.Run();
