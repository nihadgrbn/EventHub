using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Events.Queries.GetEvents
{
    public record GetEventsQuery() : IRequest<IEnumerable<EventResponse>>;

}
