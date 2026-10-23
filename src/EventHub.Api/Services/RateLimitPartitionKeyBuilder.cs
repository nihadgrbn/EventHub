using System.Security.Claims;

namespace EventHub.Api.Services;

public static class RateLimitPartitionKeyBuilder
{
    public static string Build(string policy, HttpContext context)
    {
        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "anonymous-client";
        var userId = context.User.FindFirst("sub")?.Value
                     ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var subject = string.IsNullOrWhiteSpace(userId) ? "anonymous" : $"user:{userId}";
        return $"eventhub:rate-limit:{policy}:{subject}:ip:{clientIp}";
    }
}