using FluentAssertions;
using Shared.Services;
using System.Reflection;

namespace Tests.IntegrationTests;

public class InMemoryEventInboxTests
{
    [Fact]
    public async Task TryStartAsync_ShouldBeIdempotent()
    {
        var inbox = new InMemoryEventInbox();
        var eventId = Guid.NewGuid().ToString();

        var first = await inbox.TryStartAsync(eventId, "OrderCreated", Guid.NewGuid(), "orders", 0, 10);
        var second = await inbox.TryStartAsync(eventId, "OrderCreated", Guid.NewGuid(), "orders", 0, 11);

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public async Task MarkProcessedAsync_ShouldPersistState()
    {
        var inbox = new InMemoryEventInbox();
        var eventId = Guid.NewGuid().ToString();

        await inbox.TryStartAsync(eventId, "OrderCreated", Guid.NewGuid(), "orders", 0, 10);
        await inbox.MarkProcessedAsync(eventId);

        GetState(inbox, eventId).Should().Be("processed");
    }

    [Fact]
    public async Task MarkFailedAsync_ShouldPersistState()
    {
        var inbox = new InMemoryEventInbox();
        var eventId = Guid.NewGuid().ToString();

        await inbox.TryStartAsync(eventId, "OrderCreated", Guid.NewGuid(), "orders", 0, 10);
        await inbox.MarkFailedAsync(eventId, "boom");

        GetState(inbox, eventId).Should().Be("failed");
    }

    private static string? GetState(InMemoryEventInbox inbox, string eventId)
    {
        var field = typeof(InMemoryEventInbox)
            .GetField("_events", BindingFlags.Instance | BindingFlags.NonPublic);
        var dict = field?.GetValue(inbox) as System.Collections.Concurrent.ConcurrentDictionary<string, string>;
        return dict != null && dict.TryGetValue(eventId, out var value) ? value : null;
    }
}
