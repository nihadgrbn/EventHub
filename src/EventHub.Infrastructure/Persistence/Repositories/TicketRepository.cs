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

    public async Task<IReadOnlyList<Ticket>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken)
    {
        return await _context.Tickets
            .AsNoTracking()
            .Include(ticket => ticket.Event)
            .Include(ticket => ticket.TicketType)
            .Include(ticket => ticket.Attendee)
            .Where(ticket => ticketIds.Contains(ticket.Id))
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

    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsByTicketTypeIdsAsync(
        IReadOnlyCollection<Guid> ticketTypeIds,
        CancellationToken cancellationToken)
    {
        if (ticketTypeIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticketTypeIds.Contains(ticket.TicketTypeId))
            .GroupBy(ticket => ticket.TicketTypeId)
            .ToDictionaryAsync(group => group.Key, group => group.Count(), cancellationToken);
    }

    public async Task<Ticket?> GetTicketByIdAsync(Guid id, CancellationToken cancellationToken) =>
        await _context.Tickets.Include(ticket => ticket.Event).FirstOrDefaultAsync(ticket => ticket.Id == id, cancellationToken);

    public async Task<Ticket?> GetByQrTokenHashAsync(string qrTokenHash, CancellationToken cancellationToken) =>
        await _context.Tickets.AsNoTracking().Include(ticket => ticket.Event)
            .FirstOrDefaultAsync(ticket => ticket.QrTokenHash == qrTokenHash, cancellationToken);

    public async Task<bool> TryCheckInAsync(Guid eventId, string qrTokenHash, Guid checkedInById, DateTime checkedInAt, CancellationToken cancellationToken) =>
        await _context.Tickets
            .Where(ticket => ticket.EventId == eventId && ticket.QrTokenHash == qrTokenHash && ticket.CheckedInAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ticket => ticket.CheckedInAt, checkedInAt)
                .SetProperty(ticket => ticket.CheckedInById, checkedInById), cancellationToken) == 1;
}
