using CartService.Application.Interfaces;
using CartService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<CartDbContext>();
builder.Services.AddOpenApi();
builder.Services.AddGrpcClient<Shared.Proto.OrderGrpc.OrderGrpcClient>(o =>
{
    o.Address = new Uri("http://localhost:5001");
});
// Kafka Event Producer
builder.Services.AddSingleton<Shared.Services.IEventProducer, Shared.Services.KafkaEventProducer>();

// Cart DDD services
builder.Services.AddScoped<ICartRepository, CartService.Api.Repositories.CartRepository>();
builder.Services.AddSingleton<CartService.Api.Services.ICartIntegrationEventMapper, CartService.Api.Services.CartEventMapper>();
// Outbox processor
builder.Services.AddHostedService<CartService.Infrastructure.Outbox.OutboxProcessor>();

var app = builder.Build();

app.MapControllers();
app.MapGet("/health", () => "OK");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

app.Run();