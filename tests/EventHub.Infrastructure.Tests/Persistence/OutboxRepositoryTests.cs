using EventHub.Domain.Entities;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EventHub.Infrastructure.Tests.Persistence;

public sealed class OutboxRepositoryTests
{
    [Fact]
    public async Task GetBatchAsync_ReturnsDuePendingMessagesInCreationOrder()
    {
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var now = DateTime.UtcNow;
        var older = new OutboxMessage { CreatedAt = now.AddMinutes(-2), NextAttemptAt = now.AddMinutes(-1) };
        var newer = new OutboxMessage { CreatedAt = now.AddMinutes(-1), NextAttemptAt = now.AddMinutes(-1) };
        var future = new OutboxMessage { CreatedAt = now.AddMinutes(-3), NextAttemptAt = now.AddMinutes(10) };
        context.OutboxMessages.AddRange(older, newer, future);
        await context.SaveChangesAsync();

        var repository = new OutboxRepository(context);
        var result = await repository.GetBatchAsync(10, now, CancellationToken.None);

        Assert.Equal(new[] { older.Id, newer.Id }, result.Select(message => message.Id));
    }

    [Fact]
    public async Task TryMarkProcessingAsync_IsIdempotentForConcurrentClaims()
    {
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var message = new OutboxMessage();
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();
        var repository = new OutboxRepository(context);

        Assert.True(await repository.TryMarkProcessingAsync(message.Id, CancellationToken.None));
        Assert.False(await repository.TryMarkProcessingAsync(message.Id, CancellationToken.None));
    }

    [Fact]
    public async Task MarkFailedAsync_IncrementsRetryAndStoresError()
    {
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var message = new OutboxMessage();
        context.OutboxMessages.Add(message);
        await context.SaveChangesAsync();
        var repository = new OutboxRepository(context);
        var nextAttempt = DateTime.UtcNow.AddMinutes(2);

        await repository.MarkFailedAsync(message.Id, "SMTP unavailable", nextAttempt, CancellationToken.None);
        context.ChangeTracker.Clear();
        var stored = await context.OutboxMessages.SingleAsync();

        Assert.Equal(OutboxMessageStatus.Failed, stored.Status);
        Assert.Equal(1, stored.RetryCount);
        Assert.Equal("SMTP unavailable", stored.LastError);
        Assert.Equal(nextAttempt, stored.NextAttemptAt);
    }

    private static SqliteConnection CreateConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static ApplicationDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        return new ApplicationDbContext(options);
    }
}
