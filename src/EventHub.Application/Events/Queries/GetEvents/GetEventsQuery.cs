using EventHub.Application.Common.Models;
using EventHub.Domain.Enums;
using MediatR;

namespace EventHub.Application.Events.Queries.GetEvents
{
    public record GetEventsQuery(
        string? SearchTerm = null,
        string? Location = null,
        EventCategory? Category = null,
        DateTime? DateFrom = null,
        DateTime? DateTo = null,
        string? SortBy = "date",      
        string? SortOrder = "asc",    
        int PageNumber = 1,
        int PageSize = 12) : IRequest<PaginatedList<EventResponse>>;

}
