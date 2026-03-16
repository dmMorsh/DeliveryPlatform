using CartService.Api.CompositionRoot;
using Hangfire;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Service identity
builder.AddServiceTelemetry("cart-service");

// API surface
builder.Services.AddCartServiceApi();

// Runtime flags
var settings = CartServiceSettings.From(builder.Configuration, builder.Environment);

if (settings.UseInMemory && builder.Environment.IsProduction())
    throw new InvalidOperationException("In-memory database is not allowed in production.");

// Composition root
builder.Services.AddCartServiceCore(builder.Configuration, builder.Environment, settings);

// Auth + CORS
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

// Health checks
builder.Services.AddCartServiceHealthChecks(
    settings);

var app = builder.Build();

// Auth
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

// Endpoints
app.MapControllers();
app.MapCartServiceHealthChecks();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHangfireDashboard("/hangfire");
}

app.UseHttpsRedirection();

// Startup tasks
app.RunCartServiceMigrations(settings.UseInMemory);

app.Run();
