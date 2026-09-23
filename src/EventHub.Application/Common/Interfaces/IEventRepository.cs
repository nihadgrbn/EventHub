using EventHub.Application.Organizers.Queries.GetStatistics;
using EventHub.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using EventHub.Domain.Enums;

namespace EventHub.Application.Common.Interfaces
{
    public interface IEventRepository
    {
        Task AddAsync(Event @event, CancellationToken cancellationToken);
        Task<IEnumerable<Event>> GetAllAsync(CancellationToken cancellationToken);
        Task<Event?> GetByIdAsync(Guid id,CancellationToken cancellationToken);
        void Update(Event @event);
        void Delete(Event @event);
        Task<int> CompleteExpiredPublishedEventsAsync(
            DateTime utcNow,
            CancellationToken cancellationToken);
        Task<(IEnumerable<Event> Events, int TotalCount)> GetPagedEventsAsync(
            string? searchTerm, 
            string? location, 
            EventCategory? category,
            DateTime? dateFrom,
            DateTime? dateTo,
            string? sortBy, 
            string? sortOrder,
            int pageNumber, 
            int pageSize, 
            CancellationToken cancellationToken);
        Task<OrganizerStatisticsDto> GetOrganizerStatisticsAsync(Guid organizerId, CancellationToken cancellationToken);

    }
}
