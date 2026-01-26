using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Services;

/// <summary>
/// Hosted service that ensures required Kafka topics exist on startup.
/// Topics are read from configuration key `Kafka:Topics` (comma-separated or array).
/// </summary>
public class KafkaTopicBootstrapper : IHostedService
{
    private readonly IConfiguration _config;
    private readonly ILogger<KafkaTopicBootstrapper> _logger;

    public KafkaTopicBootstrapper(IConfiguration config, ILogger<KafkaTopicBootstrapper> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var brokers = _config["Kafka:Brokers"] ?? "localhost:29092";

            // Read topics from configuration. Support both array and comma-separated string.
            var section = _config.GetSection("Kafka:Topics");
            string?[] topics;
            if (section.Exists())
            {
                // Try binder first (array in config), fallback to children values
                topics = section.GetChildren().Select(c => c.Value).Where(v => !string.IsNullOrEmpty(v)).ToArray();
                    // section.Get<string[]>() ?? 
            }
            else
            {
                topics = (_config["Kafka:Topics"] ?? string.Empty)
                    .Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToArray();
            }

            if (topics.Length == 0)
            {
                _logger.LogInformation("No Kafka topics configured for bootstrap (Kafka:Topics).");
                return;
            }

            using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = brokers }).Build();

            var specs = topics.Select(t => new TopicSpecification
            {
                Name = t,
                NumPartitions = int.TryParse(_config["Kafka:DefaultPartitions"], out var p) ? p : 3,
                ReplicationFactor = short.TryParse(_config["Kafka:DefaultReplicationFactor"], out var r) ? r : (short)1
            }).ToList();

            try
            {
                await admin.CreateTopicsAsync(specs).ConfigureAwait(false);
                _logger.LogInformation("Created Kafka topics: {Topics}", string.Join(',', topics));
            }
            catch (CreateTopicsException ex)
            {
                // Some topics might already exist; log details and ignore those errors
                foreach (var e in ex.Results)
                {
                    if (e.Error.Code == Confluent.Kafka.ErrorCode.TopicAlreadyExists)
                        _logger.LogInformation("Kafka topic already exists: {Topic}", e.Topic);
                    else
                        _logger.LogWarning("Error creating topic {Topic}: {Error}", e.Topic, e.Error.Reason);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KafkaTopicBootstrapper failed to ensure topics");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
