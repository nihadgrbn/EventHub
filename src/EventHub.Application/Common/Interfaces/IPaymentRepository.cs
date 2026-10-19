using EventHub.Domain.Entities;

namespace EventHub.Application.Common.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(Payment payment, CancellationToken cancellationToken);

    Task<Payment?> GetByIdempotencyKeyAsync(
        Guid attendeeId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<bool> TryMarkAsPaidAsync(
        Guid paymentId,
        DateTime paidAt,
        CancellationToken cancellationToken);

    Task<bool> TryMarkAsExpiredAsync(
        Guid paymentId,
        DateTime expiredAt,
        CancellationToken cancellationToken);

    Task<bool> TryMarkAsFailedAsync(
        Guid paymentId,
        DateTime failedAt,
        CancellationToken cancellationToken);

    Task<bool> TryMarkAsExpiredByCheckoutSessionIdAsync(
        string checkoutSessionId,
        DateTime expiredAt,
        CancellationToken cancellationToken);

    Task<Payment?> GetByCheckoutSessionIdAsync(
        string checkoutSessionId,
        CancellationToken cancellationToken);

    Task<Payment?> GetByPaymentIntentIdAsync(
        string paymentIntentId,
        CancellationToken cancellationToken);

    Task<bool> TryMarkAsFailedByPaymentIntentIdAsync(
        string paymentIntentId,
        DateTime failedAt,
        CancellationToken cancellationToken);

    Task<bool> TryMarkAsRefundedByPaymentIntentIdAsync(
        string paymentIntentId,
        DateTime refundedAt,
        CancellationToken cancellationToken);

    Task<Payment?> GetPaidByPaymentIntentIdAsync(
        string paymentIntentId,
        CancellationToken cancellationToken);

    Task SetCheckoutSessionAsync(
        Guid paymentId,
        string checkoutSessionId,
        string checkoutUrl,
        CancellationToken cancellationToken);

    Task SetPaymentIntentIdAsync(
        Guid paymentId,
        string paymentIntentId,
        CancellationToken cancellationToken);
}
