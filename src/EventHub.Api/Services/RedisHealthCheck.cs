using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace EventHub.Api.Services;

public sealed class RedisHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;

    public RedisHealthCheck(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var redis = _serviceProvider.GetService<IConnectionMultiplexer>();
        if (redis is null)
        {
            return HealthCheckResult.Healthy("Redis connection is not configured.");
        }

        try
        {
            var db = redis.GetDatabase();
            var latency = await db.PingAsync();
            return HealthCheckResult.Healthy($"Redis is responsive (latency: {latency.TotalMilliseconds:F1}ms).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable.", ex);
        }
    }
}
