using EventHub.Application.Common.Interfaces;
using Mapster;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace EventHub.Application.Events.Queries.GetEvents
{
    public class GetEventsQueryHandler: IRequestHandler<GetEventsQuery, IEnumerable<EventResponse>>
    {
        private readonly IEventRepository _eventRepository;
        public GetEventsQueryHandler(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }
        public async Task<IEnumerable<EventResponse>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
        {
            var events = await _eventRepository.GetAllAsync(cancellationToken);

            return events.Adapt<IEnumerable<EventResponse>>();
        }
    }
}
