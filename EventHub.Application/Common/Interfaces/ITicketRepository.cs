using EventHub.Domain.Entities;

namespace EventHub.Application.Common.Interfaces;

public interface ITicketRepository
{
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<Ticket> tickets, CancellationToken cancellationToken);
    Task<IEnumerable<Ticket>> GetTicketsByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}