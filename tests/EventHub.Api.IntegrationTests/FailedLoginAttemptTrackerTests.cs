using EventHub.Api.Services;
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
}