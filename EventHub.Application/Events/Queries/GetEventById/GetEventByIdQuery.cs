using EventHub.Application.Events.Queries.GetEvents;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Events.Queries.GetEventById
{
    public record GetEventByIdQuery(Guid Id) : IRequest<EventResponse>;
}
