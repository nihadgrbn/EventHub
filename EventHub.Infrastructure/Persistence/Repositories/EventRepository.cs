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
            return await _context.Events.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }
        public async Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _context.Events.AsNoTracking().ToListAsync(cancellationToken);
        }
    }
}
