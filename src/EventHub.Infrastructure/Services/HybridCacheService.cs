using EventHub.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Hybrid;

namespace EventHub.Infrastructure.Services;

public sealed class HybridCacheService : ICacheService
{
    private readonly HybridCache _cache;

    public HybridCacheService(HybridCache cache)
    {
        _cache = cache;
    }

    public async ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        TimeSpan? expiration = null,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var options = expiration.HasValue
            ? new HybridCacheEntryOptions
            {
                Expiration = expiration.Value,
                LocalCacheExpiration = expiration.Value
            }
            : null;

        return await _cache.GetOrCreateAsync(
            key,
            factory,
            options,
            tags,
            cancellationToken);
    }

    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public async ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveByTagAsync(tag, cancellationToken);
    }
}
