using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventHub.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;

    public TicketRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        await _context.Tickets.AddAsync(ticket, cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<Ticket> tickets,
        CancellationToken cancellationToken)
    {
        await _context.Tickets.AddRangeAsync(tickets, cancellationToken);
    }
    public async Task <IEnumerable<Ticket>> GetTicketsByUserIdAsync(Guid userId,CancellationToken cancellationToken)
    {
        return await _context.Tickets
            .Include(t => t.Event)
            .Include(t => t.TicketType)
            .Where(t => t.AttendeeId == userId)
            .OrderByDescending(t => t.PurchaseDate)
            .ToListAsync(cancellationToken);
    }
    public async Task<(IEnumerable<Ticket> Tickets, int TotalCount)> GetOrdersByOrganizerIdAsync(
    Guid organizerId, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = _context.Tickets
            .Include(t => t.Event)
            .Include(t => t.TicketType)
            .Include(t => t.Attendee)
            .Where(t => t.Event!.OrganizerId == organizerId)
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);

        var tickets = await query
            .OrderByDescending(t => t.PurchaseDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (tickets, totalCount);
    }
}
