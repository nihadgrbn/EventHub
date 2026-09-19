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
        public async Task<IActionResult> GetEvents([FromQuery] GetEventsQuery query, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = Roles.OrganizerOrAdmin)]
        public async Task<IActionResult> CreateEvent([FromBody] CreateEventCommand command, CancellationToken cancellationToken)
        {
            var eventId = await _sender.Send(command, cancellationToken);
                return CreatedAtAction(nameof(GetEventById), new { id = eventId }, eventId); 
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetEventById(Guid id, CancellationToken cancellationToken)
        {
            var query = new GetEventByIdQuery(id);
            var @event = await _sender.Send(query, cancellationToken);
            return Ok(@event);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = Roles.OrganizerOrAdmin)]
        public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventCommand command, CancellationToken cancellationToken)
        {
            await _sender.Send(command with { Id = id }, cancellationToken);
            return NoContent();
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = Roles.OrganizerOrAdmin)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateEventStatusRequest request, CancellationToken cancellationToken)
        {
            await _sender.Send(new UpdateEventStatusCommand(id, request.Status), cancellationToken);
            return NoContent();
        }

        [HttpPut("{id}/ticket-types")]
        [Authorize(Roles = Roles.OrganizerOrAdmin)]
        public async Task<IActionResult> UpdateTicketTypes(Guid id, [FromBody] List<UpdateTicketTypeDto> ticketTypes, CancellationToken cancellationToken)
        {
            await _sender.Send(new UpdateEventTicketTypesCommand(id, ticketTypes), cancellationToken);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.OrganizerOrAdmin)]
        public async Task<IActionResult> DeleteEvent(Guid id, CancellationToken cancellationToken)
        {
            await _sender.Send(new DeleteEventCommand(id), cancellationToken);
            return NoContent();
        }
    }

    public record UpdateEventStatusRequest(EventStatus Status);
}