using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using EventHub.Application.Events.Commands.CreateEvent;


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
        [HttpPost]
        public async Task <IActionResult> CreateEvent([FromBody] CreateEventCommand command)
        {
            var eventId = await _sender.Send(command);
            return CreatedAtAction(nameof(CreateEvent), new { id = eventId }, eventId);
        }

    }
}
