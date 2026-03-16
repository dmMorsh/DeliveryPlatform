using Hangfire;
using PaymentService.Api.CompositionRoot;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Service identity + logging
builder.AddServiceTelemetry("payment-service");
builder.UseExtededSerilog();

// API surface
builder.Services.AddPaymentServiceApi();

// Composition root
var settings = PaymentServiceSettings.From(builder.Configuration, builder.Environment);
builder.Services.AddPaymentServiceCore(builder.Configuration, builder.Environment, settings);

// Auth + CORS
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

// Health checks
builder.Services.AddPaymentServiceHealthChecks(
    settings);

// Rate limiting
builder.Services.AddPaymentRateLimiting();

var app = builder.Build();

// Middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseCors();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// Startup tasks
app.ValidatePaymentProviders();
app.RunPaymentServiceMigrations();

// Endpoints
app.MapControllers().RequireRateLimiting("payment-default");
app.MapGrpcService<PaymentService.Api.Grpc.PaymentGrpcService>();
app.MapPaymentServiceHealthChecks();

if (app.Environment.IsDevelopment())
    app.UseHangfireDashboard("/hangfire");

app.Run();
