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
}
