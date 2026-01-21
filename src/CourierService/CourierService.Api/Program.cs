using Microsoft.EntityFrameworkCore;
using Serilog;
using CourierService.Data;
using CourierService.Repositories;
using CourierService.Services;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.WriteTo.Console()
       .MinimumLevel.Information());

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("PostgreSQL") 
    ?? "Host=localhost;Port=5432;Database=delivery_db;Username=postgres;Password=postgres;";
builder.Services.AddDbContext<CourierDbContext>(options =>
    options.UseNpgsql(connectionString));

// Kafka Event Producer
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();

// Outbox processor
builder.Services.AddHostedService<CourierService.Infrastructure.OutboxProcessor>();

builder.Services.AddScoped<ICourierRepository, CourierRepository>();
builder.Services.AddScoped<CourierApplicationService>();
// Mapper for domain->integration events for courier
builder.Services.AddSingleton<ICourierIntegrationEventMapper, CourierEventMapper>();
// Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CourierDbContext>();
    dbContext.Database.Migrate();
    Log.Information("Database migration completed for CourierService");
}

app.Run();
