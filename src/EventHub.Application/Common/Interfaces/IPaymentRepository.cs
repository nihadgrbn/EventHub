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
}
