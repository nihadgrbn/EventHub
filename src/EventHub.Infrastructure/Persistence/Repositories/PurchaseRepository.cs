using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;

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
}
