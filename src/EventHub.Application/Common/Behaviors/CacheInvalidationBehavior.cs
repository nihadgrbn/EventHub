using EventHub.Application.Common.Interfaces;
using MediatR;

namespace EventHub.Application.Common.Behaviors;

public sealed class CacheInvalidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ICacheService _cacheService;

    public CacheInvalidationBehavior(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IInvalidatesCache invalidator)
        {
            foreach (var tag in invalidator.CacheTags)
            {
                await _cacheService.RemoveByTagAsync(tag, cancellationToken);
            }
        }

        return response;
    }
}
