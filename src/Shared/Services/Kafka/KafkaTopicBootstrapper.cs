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
    private readonly IHostEnvironment _env;

    public KafkaTopicBootstrapper(IConfiguration config, IHostEnvironment env, ILogger<KafkaTopicBootstrapper> logger)
    {
        _config = config;
        _logger = logger;
        _env = env;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var brokers = ConfigurationGuard.GetRequired(_config, _env, "Kafka:Brokers", "localhost:29092");

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

            var dlqTopic = _config["Kafka:DLQTopic"] ?? "dlq.events";
            if (!string.IsNullOrWhiteSpace(dlqTopic) && !topics.Contains(dlqTopic))
            {
                var list = topics.ToList();
                list.Add(dlqTopic);
                topics = list.ToArray();
            }

            var retryTopic = _config["Kafka:Retry:Topic"];
            if (!string.IsNullOrWhiteSpace(retryTopic) && !topics.Contains(retryTopic))
            {
                var list = topics.ToList();
                list.Add(retryTopic);
                topics = list.ToArray();
            }

            if (topics.Length == 0)
            {
                if (_env.IsProduction())
                    throw new InvalidOperationException("Kafka:Topics configuration is required in production.");

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
                    if (e.Error.Code == ErrorCode.TopicAlreadyExists)
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
