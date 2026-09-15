using EventHub.Application.Tickets.Commands.BuyTicket;
using EventHub.Application.Tickets.Queries.GetMyTickets;
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
    [HttpGet("my-tickets")]
    public async Task<IActionResult> GetMyTickets()
    {
        var query = new GetMyTicketsQuery();
        var result = await _sender.Send(query);

        return Ok(result);
    }

    [HttpPost("buy")]
    public async Task<IActionResult> BuyTicket(
        [FromBody] BuyTicketCommand command,
        CancellationToken cancellationToken)
    {
        var ticketIds = await _sender.Send(command, cancellationToken);

        return Ok(new
        {
            Message = "Tickets purchased successfully.",
            Quantity = ticketIds.Count,
            TicketIds = ticketIds
        });
    }
}
