using EventHub.Domain.Entities;

namespace EventHub.Application.Common.Interfaces;

public interface ITicketRepository
{
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken);
    Task AddRangeAsync(IEnumerable<Ticket> tickets, CancellationToken cancellationToken);
    Task<IEnumerable<Ticket>> GetTicketsByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<(IEnumerable<Ticket> Tickets, int TotalCount)> GetOrdersByOrganizerIdAsync(
    Guid organizerId, int pageNumber, int pageSize, CancellationToken cancellationToken);
}