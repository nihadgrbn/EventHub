using EventHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Common.Interfaces
{
    public interface IEventRepository
    {
        Task AddAsync(Event @event, CancellationToken cancellationToken);
        Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken);
        Task<Event?> GetByIdAsync(Guid id,CancellationToken cancellationToken);
        void Update(Event @event);
        void Delete(Event @event);
        Task<(IEnumerable<Event> Events, int TotalCount)> GetPagedEventsAsync(
            string? searchTerm, 
            string? location, 
            string? sortBy, 
            string? sortOrder,
            int pageNumber, 
            int pageSize, 
            CancellationToken cancellationToken);

    }
}
