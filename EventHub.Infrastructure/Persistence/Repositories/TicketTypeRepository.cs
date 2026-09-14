using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

public sealed class TicketTypeRepository : ITicketTypeRepository
{
    private readonly ApplicationDbContext _context;

    public TicketTypeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TicketType?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _context.TicketTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(ticketType => ticketType.Id == id, cancellationToken);
    }

    public async Task<bool> TryDecreaseAvailableQuantityAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _context.TicketTypes
            .Where(ticketType => ticketType.Id == id && ticketType.AvailableQuantity > 0)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    ticketType => ticketType.AvailableQuantity,
                    ticketType => ticketType.AvailableQuantity - 1),
                cancellationToken) == 1;
    }
}
