using EventHub.Infrastructure.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EventHub.Infrastructure.Tests.Services;

public sealed class HybridCacheServiceTests
{
    private readonly HybridCacheService _cacheService;

    public HybridCacheServiceTests()
    {
        var services = new ServiceCollection();
#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018
        var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<HybridCache>();
        _cacheService = new HybridCacheService(cache);
    }

    [Fact]
    public async Task GetOrCreateAsync_CachesResult_SecondCallDoesNotInvokeFactory()
    {
        var key = $"test:{Guid.NewGuid():N}";
        var invocationCount = 0;

        var first = await _cacheService.GetOrCreateAsync(
            key,
            token =>
            {
                Interlocked.Increment(ref invocationCount);
                return ValueTask.FromResult("hello-world");
            });

        var second = await _cacheService.GetOrCreateAsync(
            key,
            token =>
            {
                Interlocked.Increment(ref invocationCount);
                return ValueTask.FromResult("new-world");
            });

        Assert.Equal("hello-world", first);
        Assert.Equal("hello-world", second);
        Assert.Equal(1, invocationCount);
    }

    [Fact]
    public async Task RemoveAsync_EvictsKey_SubsequentCallInvokesFactory()
    {
        var key = $"test:{Guid.NewGuid():N}";
        var invocationCount = 0;

        await _cacheService.GetOrCreateAsync(
            key,
            token =>
            {
                Interlocked.Increment(ref invocationCount);
                return ValueTask.FromResult("version-1");
            });

        await _cacheService.RemoveAsync(key);

        var refreshed = await _cacheService.GetOrCreateAsync(
            key,
            token =>
            {
                Interlocked.Increment(ref invocationCount);
                return ValueTask.FromResult("version-2");
            });

        Assert.Equal("version-2", refreshed);
        Assert.Equal(2, invocationCount);
    }

    [Fact]
    public async Task RemoveByTagAsync_EvictsTaggedEntries_SubsequentCallInvokesFactory()
    {
        var tag = $"tag:{Guid.NewGuid():N}";
        var key = $"test:{Guid.NewGuid():N}";
        var invocationCount = 0;

        await _cacheService.GetOrCreateAsync(
            key,
            token =>
            {
                Interlocked.Increment(ref invocationCount);
                return ValueTask.FromResult("tagged-value");
            },
            tags: [tag]);

        await _cacheService.RemoveByTagAsync(tag);

        var refreshed = await _cacheService.GetOrCreateAsync(
            key,
            token =>
            {
                Interlocked.Increment(ref invocationCount);
                return ValueTask.FromResult("new-tagged-value");
            },
            tags: [tag]);

        Assert.Equal("new-tagged-value", refreshed);
        Assert.Equal(2, invocationCount);
    }

    [Fact]
    public async Task Concurrency_StampedeProtection_MultipleSimultaneousCallersExecuteFactoryOnlyOnce()
    {
        var key = $"stampede:{Guid.NewGuid():N}";
        var factoryExecutionCount = 0;

        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(async () =>
        {
            return await _cacheService.GetOrCreateAsync(
                key,
                async token =>
                {
                    Interlocked.Increment(ref factoryExecutionCount);
                    await Task.Delay(50, token); // simulate database latency
                    return "computed-data";
                });
        })).ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r => Assert.Equal("computed-data", r));
        Assert.Equal(1, factoryExecutionCount);
    }
}
