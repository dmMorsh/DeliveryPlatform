using AnalyticsService.Services;
using Confluent.Kafka;
using Serilog;
using Shared.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .WriteTo.File("../../logs/AnalyticsService-.log", 
           rollingInterval: RollingInterval.Day, 
           outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
       .MinimumLevel.Information());

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.AddServiceTelemetry("analytics-service");

var kafkaBrokers = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Kafka:Brokers", "localhost:29092");
builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "ready" })
    .AddKafka(new ProducerConfig { BootstrapServers = kafkaBrokers }, name: "kafka");

// Register analytics consumer
builder.Services.AddSingleton<AnalyticsEventConsumer>();
builder.Services.AddHostedService<KafkaEventConsumerHostedService<AnalyticsEventConsumer>>();
builder.Services.AddSingleton<IEventProducer, KafkaEventProducer>();
builder.Services.AddSingleton<IEventInbox, InMemoryEventInbox>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
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
