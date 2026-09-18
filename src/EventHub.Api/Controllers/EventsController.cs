using EventHub.Application.Events.Commands.CreateEvent;
using EventHub.Application.Events.Commands.DeleteEvent;
using EventHub.Application.Events.Commands.UpdateEvent;
using EventHub.Application.Events.Commands.UpdateEventStatus;
using EventHub.Application.Events.Commands.UpdateTicketTypes;
using EventHub.Domain.Enums;
using EventHub.Application.Events.Queries.GetEventById;
using EventHub.Application.Events.Queries.GetEvents;
using EventHub.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace EventHub.Api.Controllers
{
    [Authorize]
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
        [AllowAnonymous]
        public async Task<IActionResult> GetEvents([FromQuery] GetEventsQuery query)
        {
            var result = await _sender.Send(query);
            return Ok(result);
        }
        [HttpPost]
        [Authorize(Roles = Roles.OrganizerOrAdmin)]
        public async Task <IActionResult> CreateEvent([FromBody] CreateEventCommand command)
        {
            var eventId = await _sender.Send(command);
            return CreatedAtAction(nameof(CreateEvent), new { id = eventId }, eventId);
        }
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEventById(Guid id)
        {
            var query = new GetEventByIdQuery(id);
            var @event = await _sender.Send(query);

            return Ok(@event);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.OrganizerOrAdmin)]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventCommand command)
        {
            await _sender.Send(command with { Id = id });
            return NoContent();
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = Roles.OrganizerOrAdmin)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateEventStatusRequest request)
        {
            await _sender.Send(new UpdateEventStatusCommand(id, request.Status));
            return NoContent();
        }

        [HttpPut("{id}/ticket-types")]
        [Authorize(Roles = Roles.OrganizerOrAdmin)]
        public async Task<IActionResult> UpdateTicketTypes(Guid id, [FromBody] List<UpdateTicketTypeDto> ticketTypes)
        {
            await _sender.Send(new UpdateEventTicketTypesCommand(id, ticketTypes));
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.OrganizerOrAdmin)]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            await _sender.Send(new DeleteEventCommand(id));
            return NoContent();
        }

    }

    public record UpdateEventStatusRequest(EventStatus Status);
}
