using EventHub.Application.Tickets.Commands.BuyTicket;
using EventHub.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventHub.Api.Controllers;

[Authorize(Roles = Roles.Attendee)]
[ApiController]
[Route("api/[controller]")]
public sealed class TicketsController : ControllerBase
{
    private readonly ISender _sender;

    public TicketsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("buy")]
    public async Task<IActionResult> BuyTicket(
        [FromBody] BuyTicketCommand command,
        CancellationToken cancellationToken)
    {
        var ticketId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(BuyTicket),
            new { id = ticketId },
            new
            {
                Message = "Ticket purchased successfully.",
                TicketId = ticketId
            });
    }
}
