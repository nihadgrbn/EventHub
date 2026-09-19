using EventHub.Application.Common.Interfaces;
using EventHub.Application.Organizers.Queries.GetStatistics;
using EventHub.Domain.Entities;
using EventHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;


namespace EventHub.Infrastructure.Persistence.Repositories
{
    public class EventRepository: IEventRepository
    {
        private readonly ApplicationDbContext _context;
        public EventRepository(ApplicationDbContext context)
        {
            _context = context;
            
        }
        public async Task AddAsync(Event @event, CancellationToken cancellationToken)
        {
            await _context.Events.AddAsync(@event, cancellationToken);
        }
        public async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }
        public async Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Events
                .AsNoTracking()
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .ToListAsync(cancellationToken);
        }

        public void Update(Event @event)
        {
            _context.Events.Update(@event);
        }

        public void Delete(Event @event)
        {
            _context.Events.Remove(@event);
        }
        public async Task<OrganizerStatisticsDto> GetOrganizerStatisticsAsync(Guid organizerId, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var totalEvents = await _context.Events
                .CountAsync(e => e.OrganizerId == organizerId, cancellationToken);

            var upcomingEvents = await _context.Events
                .CountAsync(e => e.OrganizerId == organizerId && e.Date > now, cancellationToken);

            var ticketsQuery = _context.Tickets
                .Include(t => t.TicketType)
                .Where(t => t.Event!.OrganizerId == organizerId);

            var totalTicketsSold = await ticketsQuery.CountAsync(cancellationToken);

            var totalRevenue = await ticketsQuery
                .SumAsync(t => t.PriceAtPurchase, cancellationToken);

            return new OrganizerStatisticsDto(totalEvents, totalTicketsSold, totalRevenue, upcomingEvents);
        }

        public async Task<(IEnumerable<Event> Events, int TotalCount)> GetPagedEventsAsync(
    string? searchTerm,
    string? location,
    EventCategory? category,
    DateTime? dateFrom,
    DateTime? dateTo,
    string? sortBy,
    string? sortOrder,
    int pageNumber,
    int pageSize,
    CancellationToken cancellationToken)
        {
            var query = _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Where(e => e.Status == EventHub.Domain.Enums.EventStatus.Published)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => e.Title.Contains(searchTerm) || e.Description.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(e => e.Location.Contains(location));
            }

            if (category.HasValue)
            {
                query = query.Where(e => e.Category == category.Value);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(e => e.Date >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(e => e.Date <= dateTo.Value);
            }

            query = sortBy?.ToLower() switch
            {
                "title" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(e => e.Title) : query.OrderBy(e => e.Title),
                "date" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(e => e.Date) : query.OrderBy(e => e.Date),
                _ => query.OrderBy(e => e.Date) 
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var events = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (events, totalCount);
        }
    }
}
