using System.Collections.Concurrent;
using StackExchange.Redis;

namespace EventHub.Api.Services;

public sealed class FailedLoginAttemptTracker
{
    private static readonly TimeSpan FailureWindow = TimeSpan.FromHours(1);
    private static readonly TimeSpan BlockDuration = TimeSpan.FromHours(1);
    private const int AccountIpFailureThreshold = 5;
    private const int IpFailureThreshold = 15;
    private const string StatePrefix = "eventhub:auth:failed-login:";

    private readonly ConcurrentDictionary<string, FailureState> _states = new(StringComparer.Ordinal);
    private readonly IDatabase? _database;

    public FailedLoginAttemptTracker(IConnectionMultiplexer? redis = null)
    {
        _database = redis?.GetDatabase();
    }

    public bool TryGetRetryAfter(string ipAddress, string email, out TimeSpan retryAfter)
    {
        if (_database is not null)
        {
            return TryGetRedisRetryAfter(ipAddress, email, out retryAfter);
        }

        var now = DateTimeOffset.UtcNow;
        var accountIpBlocked = TryGetBlockedUntil(GetAccountIpKey(ipAddress, email), now, out var accountIpUntil);
        var ipBlocked = TryGetBlockedUntil(GetIpKey(ipAddress), now, out var ipUntil);

        if (!accountIpBlocked && !ipBlocked)
        {
            retryAfter = TimeSpan.Zero;
            return false;
        }

        var blockedUntil = accountIpUntil > ipUntil ? accountIpUntil : ipUntil;
        retryAfter = blockedUntil - now;
        return true;
    }

    public TimeSpan RecordFailure(string ipAddress, string email)
    {
        if (_database is not null)
        {
            return RecordRedisFailure(ipAddress, email);
        }

        var now = DateTimeOffset.UtcNow;
        var accountIpUntil = RegisterFailure(GetAccountIpKey(ipAddress, email), AccountIpFailureThreshold, now);
        var ipUntil = RegisterFailure(GetIpKey(ipAddress), IpFailureThreshold, now);
        var blockedUntil = accountIpUntil > ipUntil ? accountIpUntil : ipUntil;
        return blockedUntil > now ? blockedUntil - now : TimeSpan.Zero;
    }

    public void RecordSuccess(string ipAddress, string email)
    {
        if (_database is not null)
        {
            _database.KeyDelete(GetRedisKey(GetAccountIpKey(ipAddress, email)));
            return;
        }

        _states.TryRemove(GetAccountIpKey(ipAddress, email), out _);
    }

    private bool TryGetRedisRetryAfter(string ipAddress, string email, out TimeSpan retryAfter)
    {
        var accountIpUntil = GetRedisBlockedUntil(GetAccountIpKey(ipAddress, email));
        var ipUntil = GetRedisBlockedUntil(GetIpKey(ipAddress));
        var blockedUntil = accountIpUntil > ipUntil ? accountIpUntil : ipUntil;
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        retryAfter = blockedUntil > now
            ? TimeSpan.FromSeconds(blockedUntil - now)
            : TimeSpan.Zero;
        return retryAfter > TimeSpan.Zero;
    }

    private TimeSpan RecordRedisFailure(string ipAddress, string email)
    {
        const string script = """
                              local key = KEYS[1]
                              local count = redis.call('HINCRBY', key, 'count', 1)
                              if count == 1 then
                                  redis.call('HSET', key, 'firstFailureUnix', ARGV[1])
                              end
                              redis.call('EXPIRE', key, ARGV[2])
                              if count >= tonumber(ARGV[3]) then
                                  local blockedUntil = ARGV[1] + ARGV[2]
                                  redis.call('HSET', key, 'blockedUntilUnix', blockedUntil)
                                  return blockedUntil
                              end
                              return 0
                              """;

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var accountIpUntil = ExecuteRedisFailure(
            GetAccountIpKey(ipAddress, email), script, now, AccountIpFailureThreshold);
        var ipUntil = ExecuteRedisFailure(GetIpKey(ipAddress), script, now, IpFailureThreshold);
        var blockedUntil = Math.Max(accountIpUntil, ipUntil);
        return blockedUntil > now ? TimeSpan.FromSeconds(blockedUntil - now) : TimeSpan.Zero;
    }

    private long ExecuteRedisFailure(string stateKey, string script, long now, int threshold)
    {
        var result = _database!.ScriptEvaluate(
            script,
            new[] { (RedisKey)GetRedisKey(stateKey) },
            new RedisValue[] { now, (long)FailureWindow.TotalSeconds, threshold });
        return (long)result;
    }

    private long GetRedisBlockedUntil(string stateKey)
    {
        var value = _database!.HashGet(GetRedisKey(stateKey), "blockedUntilUnix");
        return value.HasValue && long.TryParse(value.ToString(), out var blockedUntil) ? blockedUntil : 0;
    }

    private DateTimeOffset RegisterFailure(string key, int threshold, DateTimeOffset now)
    {
        var state = _states.GetOrAdd(key, _ => new FailureState());
        lock (state)
        {
            if (state.FirstFailureAt is null || now - state.FirstFailureAt >= FailureWindow)
            {
                state.FirstFailureAt = now;
                state.Count = 0;
                state.BlockedUntil = null;
            }

            state.Count++;
            if (state.Count >= threshold)
            {
                state.BlockedUntil = now + BlockDuration;
            }

            return state.BlockedUntil ?? DateTimeOffset.MinValue;
        }
    }

    private bool TryGetBlockedUntil(string key, DateTimeOffset now, out DateTimeOffset blockedUntil)
    {
        blockedUntil = DateTimeOffset.MinValue;
        if (!_states.TryGetValue(key, out var state))
        {
            return false;
        }

        lock (state)
        {
            if (state.BlockedUntil is { } until && until > now)
            {
                blockedUntil = until;
                return true;
            }

            if (state.FirstFailureAt is { } firstFailure && now - firstFailure >= FailureWindow)
            {
                _states.TryRemove(key, out _);
            }

            return false;
        }
    }

    private static string GetAccountIpKey(string ipAddress, string email) =>
        $"account:{email.Trim().ToUpperInvariant()}|ip:{ipAddress}";

    private static string GetIpKey(string ipAddress) => $"ip:{ipAddress}";

    private static string GetRedisKey(string stateKey) => $"{StatePrefix}{stateKey}";

    private sealed class FailureState
    {
        public DateTimeOffset? FirstFailureAt { get; set; }
        public DateTimeOffset? BlockedUntil { get; set; }
        public int Count { get; set; }
    }
}