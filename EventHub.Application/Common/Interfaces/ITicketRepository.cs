using EventHub.Domain.Entities;

namespace EventHub.Application.Common.Interfaces;

public interface ITicketRepository
{
    Task AddAsync(Ticket ticket, CancellationToken cancellationToken);
}