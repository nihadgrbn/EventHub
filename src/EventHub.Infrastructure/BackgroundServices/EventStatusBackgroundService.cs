using EventHub.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.BackgroundServices;

public sealed class EventStatusBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EventStatusBackgroundService> _logger;

    public EventStatusBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<EventStatusBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await CompleteExpiredEventsAsync(stoppingToken);

            using var timer = new PeriodicTimer(PollInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CompleteExpiredEventsAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Event status background service is stopping.");
        }
    }

    private async Task CompleteExpiredEventsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
            var completedCount = await eventRepository.CompleteExpiredPublishedEventsAsync(
                DateTime.UtcNow,
                cancellationToken);

            if (completedCount > 0)
            {
                _logger.LogInformation(
                    "{Count} event(s) automatically marked as Completed.",
                    completedCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An error occurred while updating expired event statuses.");
        }
    }
}