using EventHub.Application.Common.Models;
using EventHub.Application.Events.Queries.GetEvents;
using MediatR;

namespace EventHub.Application.Events.Queries.GetOrganizerEvents
{
    public record GetOrganizerEventsQuery(
        int PageNumber = 1,
        int PageSize = 12) : IRequest<PaginatedList<EventResponse>>;
}
