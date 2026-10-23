using System.Net;
using System.Security.Claims;
using EventHub.Api.Services;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace EventHub.Api.IntegrationTests;

public sealed class RateLimitPartitionKeyTests
{
    [Fact]
    public void AuthenticatedUsersWithSameIp_GetSeparatePartitions()
    {
        var first = CreateContext("203.0.113.20", "user-1");
        var second = CreateContext("203.0.113.20", "user-2");

        var firstKey = RateLimitPartitionKeyBuilder.Build("organizer-read", first);
        var secondKey = RateLimitPartitionKeyBuilder.Build("organizer-read", second);

        Assert.NotEqual(firstKey, secondKey);
        Assert.Contains("user:user-1", firstKey);
        Assert.Contains("user:user-2", secondKey);
        Assert.Contains("ip:203.0.113.20", firstKey);
    }

    [Fact]
    public void AnonymousRequestsWithSameIp_SharePartition()
    {
        var first = CreateContext("203.0.113.21");
        var second = CreateContext("203.0.113.21");

        Assert.Equal(
            RateLimitPartitionKeyBuilder.Build("public-read", first),
            RateLimitPartitionKeyBuilder.Build("public-read", second));
    }

    private static DefaultHttpContext CreateContext(string ipAddress, string? userId = null)
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ipAddress);
        if (userId is not null)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", userId)], "Test"));
        }

        return context;
    }
}