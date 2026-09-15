using EventHub.Application.Common.Interfaces;
using EventHub.Domain.Entities;
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
        public async Task<(IEnumerable<Event> Events, int TotalCount)> GetPagedEventsAsync(
    string? searchTerm, string? location, string? sortBy, string? sortOrder,
    int pageNumber, int pageSize, CancellationToken cancellationToken)
        {
            var query = _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => e.Title.Contains(searchTerm) || e.Description.Contains(searchTerm));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(e => e.Location.Contains(location));
            }

            query = sortBy?.ToLower() switch
            {
                "title" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(e => e.Title) : query.OrderBy(e => e.Title),
                "date" => sortOrder?.ToLower() == "desc" ? query.OrderByDescending(e => e.Date) : query.OrderBy(e => e.Date),
                _ => query.OrderBy(e => e.Date) // Heç nə göndərilməsə, tarixə görə sırala (Default)
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
