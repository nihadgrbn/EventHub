using EventHub.Application.Events.Commands.CreateEvent;
using EventHub.Application.Events.Queries.GetEventById;
using EventHub.Application.Events.Queries.GetEvents;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace EventHub.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class EventsController : ControllerBase
    {
        private readonly ISender _sender;
        public EventsController(ISender sender)
        {
            _sender = sender;
            
        }
        [HttpGet]
        public async Task<IActionResult> GetAllEvents()
        {
            var query = new GetEventsQuery();
            var events = await _sender.Send(query);

            return Ok(events); 
        }
        [HttpPost]
        public async Task <IActionResult> CreateEvent([FromBody] CreateEventCommand command)
        {
            var eventId = await _sender.Send(command);
            return CreatedAtAction(nameof(CreateEvent), new { id = eventId }, eventId);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEventById(Guid id)
        {
            var query = new GetEventByIdQuery(id);
            var @event = await _sender.Send(query);

            return Ok(@event);
        }

    }
}
