using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken)
    {
        await _context.Payments.AddAsync(payment, cancellationToken);
    }

    public async Task<Payment?> GetByIdempotencyKeyAsync(
        Guid attendeeId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await _context.Payments
            .AsNoTracking()
            .Include(payment => payment.Reservations)
            .SingleOrDefaultAsync(
                payment => payment.AttendeeId == attendeeId
                    && payment.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }

    public async Task<bool> TryMarkAsPaidAsync(
        Guid paymentId,
        DateTime paidAt,
        CancellationToken cancellationToken)
    {
        return await _context.Payments
            .Where(payment => payment.Id == paymentId
                && payment.Status == PaymentStatus.Pending
                && payment.ExpiresAt > paidAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payment => payment.Status, PaymentStatus.Paid)
                .SetProperty(payment => payment.PaidAt, paidAt)
                .SetProperty(payment => payment.UpdatedAt, paidAt), cancellationToken) == 1;
    }

    public async Task<bool> TryMarkAsExpiredAsync(
        Guid paymentId,
        DateTime expiredAt,
        CancellationToken cancellationToken)
    {
        return await _context.Payments
            .Where(payment => payment.Id == paymentId
                && payment.Status == PaymentStatus.Pending
                && payment.ExpiresAt <= expiredAt)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payment => payment.Status, PaymentStatus.Expired)
                .SetProperty(payment => payment.UpdatedAt, expiredAt), cancellationToken) == 1;
    }

    public async Task<bool> TryMarkAsFailedAsync(
        Guid paymentId,
        DateTime failedAt,
        CancellationToken cancellationToken)
    {
        return await _context.Payments
            .Where(payment => payment.Id == paymentId
                && payment.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payment => payment.Status, PaymentStatus.Failed)
                .SetProperty(payment => payment.UpdatedAt, failedAt), cancellationToken) == 1;
    }

    public async Task<bool> TryMarkAsExpiredByCheckoutSessionIdAsync(
        string checkoutSessionId,
        DateTime expiredAt,
        CancellationToken cancellationToken)
    {
        return await _context.Payments
            .Where(payment => payment.StripeCheckoutSessionId == checkoutSessionId
                && payment.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payment => payment.Status, PaymentStatus.Expired)
                .SetProperty(payment => payment.UpdatedAt, expiredAt), cancellationToken) == 1;
    }

    public async Task<bool> TryMarkAsFailedByPaymentIntentIdAsync(
        string paymentIntentId,
        DateTime failedAt,
        CancellationToken cancellationToken)
    {
        return await _context.Payments
            .Where(payment => payment.StripePaymentIntentId == paymentIntentId
                && payment.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payment => payment.Status, PaymentStatus.Failed)
                .SetProperty(payment => payment.UpdatedAt, failedAt), cancellationToken) == 1;
    }

    public async Task<bool> TryMarkAsRefundedByPaymentIntentIdAsync(
        string paymentIntentId,
        DateTime refundedAt,
        CancellationToken cancellationToken)
    {
        return await _context.Payments
            .Where(payment => payment.StripePaymentIntentId == paymentIntentId
                && payment.Status == PaymentStatus.Paid)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payment => payment.Status, PaymentStatus.Refunded)
                .SetProperty(payment => payment.UpdatedAt, refundedAt), cancellationToken) == 1;
    }

    public async Task SetCheckoutSessionAsync(
        Guid paymentId,
        string checkoutSessionId,
        string checkoutUrl,
        CancellationToken cancellationToken)
    {
        await _context.Payments
            .Where(payment => payment.Id == paymentId
                && payment.Status == PaymentStatus.Pending
                && payment.StripeCheckoutSessionId == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payment => payment.StripeCheckoutSessionId, checkoutSessionId)
                .SetProperty(payment => payment.StripeCheckoutUrl, checkoutUrl)
                .SetProperty(payment => payment.UpdatedAt, DateTime.UtcNow), cancellationToken);
    }

    public async Task SetPaymentIntentIdAsync(
        Guid paymentId,
        string paymentIntentId,
        CancellationToken cancellationToken)
    {
        await _context.Payments
            .Where(payment => payment.Id == paymentId
                && payment.Status == PaymentStatus.Pending)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(payment => payment.StripePaymentIntentId, paymentIntentId)
                .SetProperty(payment => payment.UpdatedAt, DateTime.UtcNow), cancellationToken);
    }
}
