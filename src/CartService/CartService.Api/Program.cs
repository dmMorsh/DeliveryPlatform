using CartService.Api.Repositories;
using CartService.Application;
using CartService.Application.Interfaces;
using CartService.Application.Mapping;
using CartService.Infrastructure;
using CartService.Infrastructure.Outbox;
using CartService.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Proto;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// Allow using an in-memory DB for local quick tests by setting USE_INMEMORY_DB=true
var useInMemory = Environment.GetEnvironmentVariable("USE_INMEMORY_DB") == "true"
                  || string.Equals(builder.Configuration["UseInMemoryDb"], "true", StringComparison.OrdinalIgnoreCase);

if (useInMemory)
{
    builder.Services.AddDbContext<CartDbContext>(options =>
        options.UseInMemoryDatabase("orders_inmem"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQL") 
                           ?? "Host=localhost;Port=5432;Database=delivery_db;Username=postgres;Password=postgres;";
    builder.Services.AddDbContext<CartDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddGrpcClient<OrderGrpc.OrderGrpcClient>(o =>
{
    o.Address = new Uri("http://localhost:5001");
});
// Kafka Event Producer
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();

// Cart DDD services
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<ICartIntegrationEventMapper, CartEventMapper>();
// Outbox processor
builder.Services.AddHostedService<OutboxProcessor>();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => "OK");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

app.Run();