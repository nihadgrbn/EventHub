using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using EventHub.Infrastructure.Persistence;
using EventHub.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventHub.Infrastructure.Tests.Payments;

public sealed class PaymentRepositoryTests
{
    [Fact]
    public async Task TryMarkAsExpiredByCheckoutSessionIdAsync_ExpiresPendingPayment()
    {
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var attendee = CreateAttendee();
        context.Users.Add(attendee);
        var payment = new Payment
        {
            AttendeeId = attendee.Id,
            StripeCheckoutSessionId = "cs_expired",
            Status = PaymentStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        var repository = new PaymentRepository(context);
        var result = await repository.TryMarkAsExpiredByCheckoutSessionIdAsync(
            payment.StripeCheckoutSessionId,
            DateTime.UtcNow,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.Payments.SingleAsync();
        Assert.True(result);
        Assert.Equal(PaymentStatus.Expired, stored.Status);
    }

    [Fact]
    public async Task TryMarkAsFailedByPaymentIntentIdAsync_FailsPendingPayment()
    {
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var attendee = CreateAttendee();
        context.Users.Add(attendee);
        var payment = new Payment
        {
            AttendeeId = attendee.Id,
            StripePaymentIntentId = "pi_failed",
            Status = PaymentStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        var repository = new PaymentRepository(context);
        var result = await repository.TryMarkAsFailedByPaymentIntentIdAsync(
            payment.StripePaymentIntentId,
            DateTime.UtcNow,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.Payments.SingleAsync();
        Assert.True(result);
        Assert.Equal(PaymentStatus.Failed, stored.Status);
    }

    [Fact]
    public async Task TryMarkAsRefundedByPaymentIntentIdAsync_RefundsPaidPaymentOnly()
    {
        await using var connection = CreateConnection();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var attendee = CreateAttendee();
        context.Users.Add(attendee);
        var payment = new Payment
        {
            AttendeeId = attendee.Id,
            StripePaymentIntentId = "pi_refunded",
            Status = PaymentStatus.Paid,
            PaidAt = DateTime.UtcNow.AddMinutes(-1),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        context.Payments.Add(payment);
        await context.SaveChangesAsync();

        var repository = new PaymentRepository(context);
        var result = await repository.TryMarkAsRefundedByPaymentIntentIdAsync(
            payment.StripePaymentIntentId,
            DateTime.UtcNow,
            CancellationToken.None);

        context.ChangeTracker.Clear();
        var stored = await context.Payments.SingleAsync();
        Assert.True(result);
        Assert.Equal(PaymentStatus.Refunded, stored.Status);
    }

    private static User CreateAttendee() => new()
    {
        FirstName = "Test",
        LastName = "Attendee",
        Email = $"{Guid.NewGuid():N}@example.com",
        PasswordHash = "test",
        Role = "Attendee"
    };

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
