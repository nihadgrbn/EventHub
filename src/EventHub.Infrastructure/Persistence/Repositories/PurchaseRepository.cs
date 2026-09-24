using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

public sealed class PurchaseRepository : IPurchaseRepository
{
    private readonly ApplicationDbContext _context;

    public PurchaseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Purchase purchase, CancellationToken cancellationToken)
    {
        await _context.Purchases.AddAsync(purchase, cancellationToken);
    }

    public async Task<Purchase?> GetByIdempotencyKeyAsync(
        Guid attendeeId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        return await _context.Purchases
            .AsNoTracking()
            .Include(purchase => purchase.Tickets)
            .SingleOrDefaultAsync(
                purchase => purchase.AttendeeId == attendeeId
                    && purchase.IdempotencyKey == idempotencyKey,
                cancellationToken);
    }
}
