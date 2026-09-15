using MediatR;

namespace EventHub.Application.Events.Queries.GetEvents
{
    public record GetEventsQuery() : IRequest<IEnumerable<EventResponse>>;

}
