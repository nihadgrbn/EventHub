using System.Text.Json;
using EventHub.Application.Common.Interfaces;
using EventHub.Application.Tickets.Events;
using EventHub.Domain.Entities;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventHub.Infrastructure.Services;

public sealed class OutboxProcessorBackgroundService : BackgroundService
{
    private const int BatchSize = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;

    public OutboxProcessorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Outbox processing cycle failed.");
                await Task.Delay(PollInterval, stoppingToken);
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var messages = await outbox.GetBatchAsync(BatchSize, DateTime.UtcNow, cancellationToken);

        foreach (var message in messages)
        {
            if (!await outbox.TryMarkProcessingAsync(message.Id, cancellationToken))
            {
                continue;
            }

            try
            {
                await using var messageScope = _scopeFactory.CreateAsyncScope();
                var publisher = messageScope.ServiceProvider.GetRequiredService<IPublisher>();
                await PublishAsync(publisher, message, cancellationToken);
                await outbox.MarkSentAsync(message.Id, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var retryCount = message.RetryCount + 1;
                var delay = TimeSpan.FromMinutes(Math.Min(Math.Pow(2, retryCount), 60));
                var nextAttemptAt = DateTime.UtcNow.Add(delay);
                var error = exception.Message[..Math.Min(exception.Message.Length, 2_000)];
                await outbox.MarkFailedAsync(message.Id, error, nextAttemptAt, cancellationToken);
                _logger.LogError(exception, "Outbox message {MessageId} failed; retry {RetryCount}.", message.Id, retryCount);
            }
        }
    }

    private static async Task PublishAsync(
        IPublisher publisher,
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Type == nameof(PurchaseReceiptEvent))
        {
            var notification = JsonSerializer.Deserialize<PurchaseReceiptEvent>(message.Payload)
                ?? throw new InvalidOperationException("Outbox payload is invalid.");
            await publisher.Publish(notification, cancellationToken);
            return;
        }

        throw new InvalidOperationException($"Unsupported outbox message type: {message.Type}");
    }
}
