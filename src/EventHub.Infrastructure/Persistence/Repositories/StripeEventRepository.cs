using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

public sealed class StripeEventRepository : IStripeEventRepository
{
    private readonly ApplicationDbContext _context;

    public StripeEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> TryMarkAsProcessedAsync(
        string eventId,
        CancellationToken cancellationToken)
    {
        FormattableString command = $"INSERT INTO \"ProcessedStripeEvents\" (\"Id\", \"CreatedAt\", \"EventId\") VALUES ({Guid.NewGuid()}, {DateTime.UtcNow}, {eventId}) ON CONFLICT (\"EventId\") DO NOTHING";

        return await _context.Database.ExecuteSqlInterpolatedAsync(
            command,
            cancellationToken) == 1;
    }
}
