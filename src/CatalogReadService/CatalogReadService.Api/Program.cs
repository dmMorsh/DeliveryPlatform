using CatalogReadService.Application.Interfaces;
using CatalogReadService.Application.MediatR;
using CatalogReadService.Infrastructure.ReadStore;
using CatalogReadService.Infrastructure.Repositories;
using Confluent.Kafka;
using Elastic.Clients.Elasticsearch;
using MediatR;
using Shared.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceTelemetry("catalog-read-service");

builder.Services.AddControllers();
builder.Services.AddMediatR(typeof(ApplicationMarker).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddScoped<IProductReadRepository, ProductReadRepository>();

// Elasticsearch client
var esUrl = builder.Configuration.GetValue<string>("Elasticsearch:Url", "http://localhost:9200");
var esSettings = new ElasticsearchClientSettings(new Uri(esUrl));
builder.Services.AddSingleton(new ElasticsearchClient(esSettings));

// Read-store projector and consumer
builder.Services.AddScoped<ProductReadProjector>();
builder.Services.AddSingleton<ProductReadProjectionConsumer>(sp =>
    new ProductReadProjectionConsumer(
        builder.Configuration,
        builder.Environment,
        sp.GetRequiredService<ILogger<ProductReadProjectionConsumer>>(),
        sp.GetRequiredService<IServiceScopeFactory>(),
        sp.GetRequiredService<IEventProducer>()));
builder.Services.AddHostedService<KafkaEventConsumerHostedService<ProductReadProjectionConsumer>>();

// Redis connection
var redisConnection = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Redis:Connection", "localhost:6379");
try
{
    var redisOptions = ConfigurationOptions.Parse(redisConnection);
    redisOptions.AbortOnConnectFail = false;
    var mux = ConnectionMultiplexer.Connect(redisOptions);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);
}
catch (Exception ex)
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        throw new InvalidOperationException($"Failed to connect to Redis at {redisConnection}", ex));
}

// Kafka producer for retry/DLQ
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
builder.Services.AddHostedService<KafkaTopicBootstrapper>();

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");
builder.Services.AddHealthChecks()
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBrokers }, name: "kafka", tags: new[] { "ready" })
    .AddRedis(redisConnection, name: "redis", tags: new[] { "ready" });

// Auth
builder.AddExtendedAuthentication();
builder.Services.AddAuthorization();
builder.AddExtendedCors();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors();

app.MapControllers();
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = reg => reg.Tags.Contains("ready")
});

app.Run();
