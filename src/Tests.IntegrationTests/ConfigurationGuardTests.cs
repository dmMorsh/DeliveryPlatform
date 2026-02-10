using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Shared.Services;

namespace Tests.IntegrationTests;

public class ConfigurationGuardTests
{
    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "/";
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values!).Build();

    [Fact]
    public void GetRequired_Dev_UsesDefault()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Development };
        var config = BuildConfig(new Dictionary<string, string?>());

        var value = ConfigurationGuard.GetRequired(config, env, "Kafka:Brokers", "localhost:29092");

        value.Should().Be("localhost:29092");
    }

    [Fact]
    public void GetRequired_Prod_ThrowsWhenMissing()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var config = BuildConfig(new Dictionary<string, string?>());

        var action = () => ConfigurationGuard.GetRequired(config, env, "Kafka:Brokers");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Kafka:Brokers configuration is required in production.");
    }

    [Fact]
    public void GetRequiredArray_Dev_UsesDefault()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Development };
        var config = BuildConfig(new Dictionary<string, string?>());

        var value = ConfigurationGuard.GetRequiredArray(config, env, "Cors:AllowedOrigins", new[] { "http://localhost" });

        value.Should().BeEquivalentTo(new[] { "http://localhost" });
    }

    [Fact]
    public void GetRequiredArray_Prod_ThrowsWhenMissing()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var config = BuildConfig(new Dictionary<string, string?>());

        var action = () => ConfigurationGuard.GetRequiredArray(config, env, "Cors:AllowedOrigins");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cors:AllowedOrigins configuration is required in production.");
    }

    [Fact]
    public void GetRequiredArray_ReturnsConfiguredValues()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.example.com",
                ["Cors:AllowedOrigins:1"] = "https://admin.example.com"
            })
            .Build();

        var value = ConfigurationGuard.GetRequiredArray(config, env, "Cors:AllowedOrigins");

        value.Should().BeEquivalentTo(new[] { "https://app.example.com", "https://admin.example.com" });
    }

    [Fact]
    public void GetRequiredConnectionString_Dev_UsesDefault()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Development };
        var config = BuildConfig(new Dictionary<string, string?>());

        var value = ConfigurationGuard.GetRequiredConnectionString(config, env, "Default", "Host=localhost;");

        value.Should().Be("Host=localhost;");
    }

    [Fact]
    public void GetRequiredConnectionString_Prod_ThrowsWhenMissing()
    {
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var config = BuildConfig(new Dictionary<string, string?>());

        var action = () => ConfigurationGuard.GetRequiredConnectionString(config, env, "Default");

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("ConnectionStrings:Default configuration is required in production.");
    }
}
