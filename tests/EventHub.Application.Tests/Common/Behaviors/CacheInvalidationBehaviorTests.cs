using EventHub.Application.Common.Behaviors;
using EventHub.Application.Common.Interfaces;
using MediatR;
using Moq;
using Xunit;

namespace EventHub.Application.Tests.Common.Behaviors;

public sealed class CacheInvalidationBehaviorTests
{
    private readonly Mock<ICacheService> _cacheService = new();

    public record TestCommand(Guid Id) : IRequest<string>, IInvalidatesCache
    {
        public IReadOnlyCollection<string> CacheTags => [$"event:{Id}", "events"];
    }

    public record NormalCommand(Guid Id) : IRequest<string>;

    [Fact]
    public async Task Handle_WhenRequestImplementsIInvalidatesCache_RemovesAllTags()
    {
        var behavior = new CacheInvalidationBehavior<TestCommand, string>(_cacheService.Object);
        var eventId = Guid.NewGuid();
        var command = new TestCommand(eventId);

        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult("success"),
            CancellationToken.None);

        Assert.Equal("success", result);
        _cacheService.Verify(c => c.RemoveByTagAsync($"event:{eventId}", It.IsAny<CancellationToken>()), Times.Once);
        _cacheService.Verify(c => c.RemoveByTagAsync("events", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRequestDoesNotImplementIInvalidatesCache_DoesNotCallRemoveByTag()
    {
        var behavior = new CacheInvalidationBehavior<NormalCommand, string>(_cacheService.Object);
        var command = new NormalCommand(Guid.NewGuid());

        var result = await behavior.Handle(
            command,
            (ct) => Task.FromResult("success"),
            CancellationToken.None);

        Assert.Equal("success", result);
        _cacheService.Verify(c => c.RemoveByTagAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
