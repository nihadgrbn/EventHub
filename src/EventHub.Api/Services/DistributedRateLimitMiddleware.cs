using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;

namespace EventHub.Api.Services;

public sealed class DistributedRateLimitMiddleware
{
    private static readonly IReadOnlyDictionary<string, int> PolicyLimits =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["global"] = 100,
            ["auth-strict"] = 5,
            ["session"] = 20,
            ["public-read"] = 60,
            ["media-upload"] = 5,
            ["reservation-strict"] = 10,
            ["ticket-qr"] = 20,
            ["ticket-check-in"] = 30,
            ["organizer-read"] = 60,
            ["admin-read"] = 120,
            ["webhook"] = 100
        };

    private const int WindowSeconds = 60;
    private const long WindowMilliseconds = WindowSeconds * 1000L;

    private const string SlidingWindowScript = """
                                                  local now = tonumber(ARGV[1])
                                                  local window = tonumber(ARGV[2])
                                                  local limit = tonumber(ARGV[3])
                                                  local member = ARGV[4]
                                                  local windowStart = now - window

                                                  redis.call('ZREMRANGEBYSCORE', KEYS[1], 0, windowStart)
                                                  local count = redis.call('ZCARD', KEYS[1])

                                                  if count >= limit then
                                                      local oldest = redis.call('ZRANGE', KEYS[1], 0, 0, 'WITHSCORES')
                                                      local retryAfter = window
                                                      if oldest[2] then
                                                          retryAfter = tonumber(oldest[2]) + window - now
                                                      end
                                                      return {0, math.max(1, retryAfter)}
                                                  end

                                                  redis.call('ZADD', KEYS[1], now, member)
                                                  redis.call('PEXPIRE', KEYS[1], window * 2)
                                                  return {1, 0}
                                               """;

    private readonly RequestDelegate _next;
    private readonly IDatabase? _database;
    private readonly ILogger<DistributedRateLimitMiddleware> _logger;

    public DistributedRateLimitMiddleware(
        RequestDelegate next,
        IServiceProvider services,
        ILogger<DistributedRateLimitMiddleware> logger)
    {
        _next = next;
        _database = services.GetService<IConnectionMultiplexer>()?.GetDatabase();
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_database is null || context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        var policy = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
                     ?? "global";
        if (!PolicyLimits.TryGetValue(policy, out var permitLimit))
        {
            _logger.LogWarning("No distributed rate limit configuration exists for policy {Policy}.", policy);
            await _next(context);
            return;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var key = RateLimitPartitionKeyBuilder.Build(policy, context);
        var member = $"{now}:{Guid.NewGuid():N}";

        try
        {
            var result = (RedisResult[])(await _database.ScriptEvaluateAsync(
                SlidingWindowScript,
                new[] { (RedisKey)key },
                new RedisValue[] { now, WindowMilliseconds, permitLimit, member }))!;
            var allowed = (long)result[0] == 1;

            if (!allowed)
            {
                var retryAfterMilliseconds = (long)result[1];
                await WriteRejectedResponseAsync(context, retryAfterMilliseconds);
                return;
            }
        }
        catch (RedisException exception)
        {
            _logger.LogError(exception,
                "Redis rate limiting failed for {Policy}; allowing request to preserve availability.", policy);
        }

        await _next(context);
    }

    private static async Task WriteRejectedResponseAsync(HttpContext context, long retryAfterMilliseconds)
    {
        var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfterMilliseconds / 1000d));
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.ContentType = "application/problem+json";
        context.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

        var problem = new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-4",
            title = "Too Many Requests",
            status = StatusCodes.Status429TooManyRequests,
            detail = "Rate limit exceeded. Please try again later.",
            retryAfterSeconds
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}