using EventHub.Api.Services;
using StackExchange.Redis;
using Xunit;

namespace EventHub.Api.IntegrationTests;

public sealed class FailedLoginAttemptTrackerTests
{
    [Fact]
    public void AccountAndIpPair_IsBlockedAfterFiveFailures()
    {
        var tracker = new FailedLoginAttemptTracker();

        for (var attempt = 0; attempt < 4; attempt++)
        {
            Assert.Equal(TimeSpan.Zero, tracker.RecordFailure("203.0.113.10", "user@example.com"));
        }

        var retryAfter = tracker.RecordFailure("203.0.113.10", "user@example.com");

        Assert.True(retryAfter > TimeSpan.Zero);
        Assert.True(tracker.TryGetRetryAfter("203.0.113.10", "user@example.com", out var blockedFor));
        Assert.True(blockedFor > TimeSpan.Zero);
    }

    [Fact]
    public void SuccessfulLogin_ClearsAccountAndIpPairFailures()
    {
        var tracker = new FailedLoginAttemptTracker();

        tracker.RecordFailure("203.0.113.11", "user@example.com");
        tracker.RecordSuccess("203.0.113.11", "user@example.com");

        Assert.False(tracker.TryGetRetryAfter("203.0.113.11", "user@example.com", out _));
    }

    [Fact]
    public void WhenRedisThrowsException_FallsBackToInMemoryTracking()
    {
        var options = ConfigurationOptions.Parse("127.0.0.1:1,abortConnect=false,connectTimeout=50,syncTimeout=50");
        using var disconnectedRedis = ConnectionMultiplexer.Connect(options);
        var tracker = new FailedLoginAttemptTracker(disconnectedRedis);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            Assert.Equal(TimeSpan.Zero, tracker.RecordFailure("203.0.113.99", "fallback@example.com"));
        }

        var retryAfter = tracker.RecordFailure("203.0.113.99", "fallback@example.com");

        Assert.True(retryAfter > TimeSpan.Zero);
        Assert.True(tracker.TryGetRetryAfter("203.0.113.99", "fallback@example.com", out var blockedFor));
        Assert.True(blockedFor > TimeSpan.Zero);
    }
}
